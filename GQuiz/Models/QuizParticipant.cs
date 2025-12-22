namespace GQuiz.Models
{
    public class QuizParticipant
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TotalScore { get; set; }
        public int TabSwitchCount { get; set; }
        public bool IsFlagged { get; set; } // flagged for cheating
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public QuizSession Session { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}