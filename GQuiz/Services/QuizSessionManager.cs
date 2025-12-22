using System.Collections.Concurrent;
using GQuiz.Models;

namespace GQuiz.Services
{
    public class SessionState
    {
        public int SessionId { get; set; }
        public int CurrentQuestionIndex { get; set; } = -1;
        public DateTime? QuestionStartTime { get; set; }
        public ConcurrentDictionary<int, int> ParticipantScores { get; set; } = new();
        public ConcurrentDictionary<int, int> TabSwitchCounts { get; set; } = new();
        public CancellationTokenSource? TimerCancellation { get; set; }
        public List<Question> Questions { get; set; } = new();
    }

    public class QuizSessionManager
    {
        private readonly ConcurrentDictionary<int, SessionState> _activeSessions = new();

        public SessionState? GetSession(int sessionId)
        {
            _activeSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public SessionState CreateSession(int sessionId, List<Question> questions)
        {
            var state = new SessionState
            {
                SessionId = sessionId,
                Questions = questions
            };
            _activeSessions[sessionId] = state;
            return state;
        }

        public void RemoveSession(int sessionId)
        {
            if (_activeSessions.TryRemove(sessionId, out var session))
            {
                session.TimerCancellation?.Cancel();
                session.TimerCancellation?.Dispose();
            }
        }

        public void RecordTabSwitch(int sessionId, int userId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.TabSwitchCounts.AddOrUpdate(userId, 1, (_, count) => count + 1);
            }
        }

        public void UpdateScore(int sessionId, int userId, int points)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.ParticipantScores.AddOrUpdate(userId, points, (_, score) => score + points);
            }
        }

        public Dictionary<int, int> GetLeaderboard(int sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                return session.ParticipantScores
                    .OrderByDescending(x => x.Value)
                    .ToDictionary(x => x.Key, x => x.Value);
            }
            return new Dictionary<int, int>();
        }
    }
}