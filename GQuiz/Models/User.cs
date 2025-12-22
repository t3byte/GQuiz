namespace GQuiz.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsHost { get; set; } // true for admin/host
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<QuizSession> HostedSessions { get; set; } = new List<QuizSession>();
        public ICollection<QuizParticipant> Participations { get; set; } = new List<QuizParticipant>();
    }
}