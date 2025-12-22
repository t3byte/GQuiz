namespace GQuiz.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();
    }
}