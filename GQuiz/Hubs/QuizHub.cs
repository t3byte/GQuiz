using Microsoft.AspNetCore.SignalR;
using GQuiz.Data;
using GQuiz.Services;
using GQuiz.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GQuiz.Hubs
{
    public class QuizHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly QuizSessionManager _sessionManager;
        private readonly ILogger<QuizHub> _logger;
        private static readonly ConcurrentDictionary<string, int> _connectionToUser = new();
        private static readonly ConcurrentDictionary<string, int> _connectionToSession = new();

        public QuizHub(AppDbContext context, QuizSessionManager sessionManager, ILogger<QuizHub> logger)
        {
            _context = context;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task JoinSession(int sessionId, int userId)
        {
            _logger.LogInformation("JoinSession called: connection={ConnectionId}, sessionId={SessionId}, userId={UserId}", Context.ConnectionId, sessionId, userId);

            var session = await _context.QuizSessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("JoinSession: session not found {SessionId}", sessionId);
                return;
            }

            // Store connection mapping
            _connectionToUser[Context.ConnectionId] = userId;
            _connectionToSession[Context.ConnectionId] = sessionId;

            // Add to SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
            _logger.LogInformation("Added connection {ConnectionId} to group Session_{SessionId}", Context.ConnectionId, sessionId);

            // Notify others
            await Clients.Group($"Session_{sessionId}").SendAsync("ParticipantJoined", userId);
            _logger.LogInformation("ParticipantJoined sent to group Session_{SessionId} for user {UserId}", sessionId, userId);

            // Acknowledge to caller that join succeeded
            await Clients.Caller.SendAsync("JoinedSession", sessionId);
            _logger.LogInformation("Sent JoinedSession ack to connection {ConnectionId}", Context.ConnectionId);
        }

        public async Task ReportTabSwitch(int sessionId, int userId)
        {
            _logger.LogInformation("ReportTabSwitch: session={SessionId} user={UserId}", sessionId, userId);
            _sessionManager.RecordTabSwitch(sessionId, userId);

            var participant = await _context.QuizParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId);

            if (participant != null)
            {
                participant.TabSwitchCount++;
                if (participant.TabSwitchCount >= 3)
                {
                    participant.IsFlagged = true;
                }
                await _context.SaveChangesAsync();

                // Notify host of tab switch
                await Clients.Group($"Session_{sessionId}")
                    .SendAsync("TabSwitchDetected", userId, participant.TabSwitchCount);
                _logger.LogInformation("TabSwitchDetected sent for session={SessionId} user={UserId} count={Count}", sessionId, userId, participant.TabSwitchCount);

                // If threshold exceeded, kick participant from session and notify host
                if (participant.TabSwitchCount >= 3)
                {
                    // Find connections for this user in this session and notify them
                    var connections = _connectionToUser
                        .Where(kv => kv.Value == userId && _connectionToSession.TryGetValue(kv.Key, out var s) && s == sessionId)
                        .Select(kv => kv.Key)
                        .ToList();

                    foreach (var connId in connections)
                    {
                        try
                        {
                            await Clients.Client(connId).SendAsync("Kicked", "Exceeded tab switch limit");
                            // Remove from SignalR group so they no longer receive session events
                            await Groups.RemoveFromGroupAsync(connId, $"Session_{sessionId}");
                            _logger.LogInformation("Kicked connection {ConnectionId} for user {UserId} from session {SessionId}", connId, userId, sessionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error notifying or removing connection {ConnectionId} for kick", connId);
                        }
                    }

                    // Notify host UI that participant was kicked
                    await Clients.Group($"Session_{sessionId}").SendAsync("ParticipantKicked", userId, participant.TabSwitchCount);
                    _logger.LogInformation("ParticipantKicked sent for session={SessionId} user={UserId}", sessionId, userId);
                }
            }
        }

        public async Task SubmitAnswer(int sessionId, int userId, int questionId, string answer)
        {
            _logger.LogInformation("SubmitAnswer: session={SessionId} user={UserId} question={QuestionId} answer={Answer}", sessionId, userId, questionId, answer);

            var state = _sessionManager.GetSession(sessionId);
            if (state == null || state.QuestionStartTime == null)
            {
                _logger.LogWarning("SubmitAnswer ignored: no active question for session {SessionId}", sessionId);
                return;
            }

            var responseTime = DateTime.UtcNow - state.QuestionStartTime.Value;

            // Run answer evaluation in parallel (PDC concept)
            await Task.Run(async () =>
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null) return;

                var isCorrect = answer.Equals(question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                var pointsAwarded = isCorrect ? question.Points : 0;

                // Save answer
                var answerRecord = new Answer
                {
                    SessionId = sessionId,
                    UserId = userId,
                    QuestionId = questionId,
                    SelectedAnswer = answer,
                    IsCorrect = isCorrect,
                    PointsAwarded = pointsAwarded,
                    ResponseTime = responseTime
                };

                _context.Answers.Add(answerRecord);

                // Update participant score
                var participant = await _context.QuizParticipants
                    .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId);

                if (participant != null)
                {
                    participant.TotalScore += pointsAwarded;
                }

                await _context.SaveChangesAsync();

                // Update in-memory score
                _sessionManager.UpdateScore(sessionId, userId, pointsAwarded);

                // Send result to user
                await Clients.Client(Context.ConnectionId).SendAsync("AnswerResult", isCorrect, pointsAwarded);
                _logger.LogInformation("AnswerResult sent to connection {ConnectionId}: correct={IsCorrect} points={Points}", Context.ConnectionId, isCorrect, pointsAwarded);
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionToSession.TryRemove(Context.ConnectionId, out var sessionId) &&
                _connectionToUser.TryRemove(Context.ConnectionId, out var userId))
            {
                _logger.LogInformation("Connection disconnected {ConnectionId} session={SessionId} user={UserId}", Context.ConnectionId, sessionId, userId);
                await Clients.Group($"Session_{sessionId}").SendAsync("ParticipantLeft", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task BroadcastQuestion(int sessionId, object question)
        {
            _logger.LogInformation("BroadcastQuestion invoked for session={SessionId} question={Question}", sessionId, question);
            await Clients.Group($"Session_{sessionId}").SendAsync("BroadcastQuestion", question);
        }

        public async Task EndQuiz(int sessionId)
        {
            _logger.LogInformation("EndQuiz invoked for session={SessionId}", sessionId);
            await Clients.Group($"Session_{sessionId}").SendAsync("EndQuiz");
        }

        // Allow host to trigger QuizStarted via hub when desired
        public async Task HostStartQuiz(int sessionId)
        {
            _logger.LogInformation("HostStartQuiz invoked for session={SessionId}", sessionId);
            await Clients.Group($"Session_{sessionId}").SendAsync("QuizStarted");
        }
    }
}