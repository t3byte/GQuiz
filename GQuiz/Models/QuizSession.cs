namespace GQuiz.Models
{
    public enum SessionStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public class QuizSession
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int HostId { get; set; }
        public string SessionCode { get; set; } = string.Empty; // 6-digit join code
        public SessionStatus Status { get; set; } = SessionStatus.NotStarted;
        public int? CurrentQuestionId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Quiz Quiz { get; set; } = null!;
        public User Host { get; set; } = null!;
        public ICollection<QuizParticipant> Participants { get; set; } = new List<QuizParticipant>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}