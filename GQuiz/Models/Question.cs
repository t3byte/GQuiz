namespace GQuiz.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty; // A, B, C, or D
        public int TimeLimit { get; set; } = 30; // seconds
        public int Points { get; set; } = 10;
        public int OrderIndex { get; set; }

        // Navigation property
        public Quiz Quiz { get; set; } = null!;
    }
}