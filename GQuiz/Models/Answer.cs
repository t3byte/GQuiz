namespace GQuiz.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty; // A, B, C, or D
        public bool IsCorrect { get; set; }
        public int PointsAwarded { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public QuizSession Session { get; set; } = null!;
    }
}