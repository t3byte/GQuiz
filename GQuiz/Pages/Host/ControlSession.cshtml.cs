using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;
using GQuiz.Services;
using Microsoft.AspNetCore.SignalR;
using GQuiz.Hubs;

namespace GQuiz.Pages.Host
{
    public class ControlSessionModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly QuizSessionManager _sessionManager;
        private readonly IHubContext<QuizHub> _hubContext;

        public ControlSessionModel(AppDbContext context, QuizSessionManager sessionManager, IHubContext<QuizHub> hubContext)
        {
            _context = context;
            _sessionManager = sessionManager;
            _hubContext = hubContext;
        }

        public QuizSession? Session { get; set; }
        public int SessionId { get; set; }
        public bool IsAutomated { get; set; }

        public async Task<IActionResult> OnGetAsync(int sessionId, bool automated = false)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");

            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            SessionId = sessionId;
            IsAutomated = automated;
            Session = await _context.QuizSessions
                .Include(s => s.Quiz)
                    .ThenInclude(q => q.Questions)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.HostId == userId.Value);

            if (Session == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            // Initialize session state if not exists
            var state = _sessionManager.GetSession(sessionId);
            if (state == null)
            {
                _sessionManager.CreateSession(sessionId, Session.Quiz.Questions.OrderBy(q => q.OrderIndex).ToList());
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartAsync(int sessionId)
        {
            var session = await _context.QuizSessions
                .Include(s => s.Quiz)
                    .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session != null)
            {
                session.Status = SessionStatus.InProgress;
                session.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Ensure in-memory session state exists so students joining after start are handled
                var state = _sessionManager.GetSession(sessionId);
                if (state == null)
                {
                    _sessionManager.CreateSession(sessionId, session.Quiz.Questions.OrderBy(q => q.OrderIndex).ToList());
                }

                await _hubContext.Clients.Group($"Session_{sessionId}").SendAsync("QuizStarted");
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostBroadcastQuestionAsync(int sessionId, int questionId)
        {
            var state = _sessionManager.GetSession(sessionId);
            if (state == null)
            {
                return BadRequest(new { success = false, error = "Session not found" });
            }

            // Cancel any existing timer
            state.TimerCancellation?.Cancel();
            state.TimerCancellation?.Dispose();
            state.TimerCancellation = new CancellationTokenSource();
            var cts = state.TimerCancellation;

            // Set question start time on server
            state.QuestionStartTime = DateTime.UtcNow;

            // Update DB session current question
            var session = await _context.QuizSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.CurrentQuestionId = questionId;
                await _context.SaveChangesAsync();
            }

            // Find question details
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                return BadRequest(new { success = false, error = "Question not found" });
            }

            // Build DTO to send to clients (camelCase expected by JS)
            var questionDto = new
            {
                id = question.Id,
                orderIndex = question.OrderIndex,
                text = question.Text,
                optionA = question.OptionA,
                optionB = question.OptionB,
                optionC = question.OptionC,
                optionD = question.OptionD,
                correctAnswer = question.CorrectAnswer,
                timeLimit = question.TimeLimit,
                points = question.Points
            };

            // Broadcast question with server timestamp (ISO 8601)
            var startTimeIso = state.QuestionStartTime.Value.ToString("o");
            await _hubContext.Clients.Group($"Session_{sessionId}").SendAsync("BroadcastQuestion", questionDto, startTimeIso);

            // Schedule server-side end of question broadcast
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(question.TimeLimit * 1000, cts.Token);
                    // Mark question end: notify clients
                    await _hubContext.Clients.Group($"Session_{sessionId}").SendAsync("QuestionEnded", sessionId, questionId);
                    // Optionally update session state
                    state.QuestionStartTime = null;
                }
                catch (TaskCanceledException)
                {
                    // Timer was canceled (e.g., next question pressed)
                }
            });

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEndAsync(int sessionId)
        {
            var session = await _context.QuizSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Status = SessionStatus.Completed;
                session.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _sessionManager.RemoveSession(sessionId);
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetParticipantsAsync(int sessionId)
        {
            var participants = await _context.QuizParticipants
                .Where(p => p.SessionId == sessionId)
                .Include(p => p.User)
                .Select(p => new
                {
                    userId = p.UserId,
                    username = p.User.Username,
                    score = p.TotalScore,
                    isFlagged = p.IsFlagged,
                    tabSwitches = p.TabSwitchCount
                })
                .ToListAsync();

            return new JsonResult(participants);
        }

        public async Task<IActionResult> OnGetLeaderboardAsync(int sessionId)
        {
            var leaderboard = await _context.QuizParticipants
                .Where(p => p.SessionId == sessionId)
                .Include(p => p.User)
                .OrderByDescending(p => p.TotalScore)
                .Select(p => new
                {
                    userId = p.UserId,
                    username = p.User.Username,
                    score = p.TotalScore
                })
                .ToListAsync();

            return new JsonResult(leaderboard);
        }
    }
}