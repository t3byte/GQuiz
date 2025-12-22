using Microsoft.EntityFrameworkCore;
using GQuiz.Models;

namespace GQuiz.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuizSession> QuizSessions { get; set; }
        public DbSet<QuizParticipant> QuizParticipants { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Quiz configuration
            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // Question configuration
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(q => q.Quiz)
                    .WithMany(qz => qz.Questions)
                    .HasForeignKey(q => q.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // QuizSession configuration
            modelBuilder.Entity<QuizSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionCode).IsUnique();

                entity.HasOne(s => s.Quiz)
                    .WithMany(q => q.Sessions)
                    .HasForeignKey(s => s.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Host)
                    .WithMany(u => u.HostedSessions)
                    .HasForeignKey(s => s.HostId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // QuizParticipant configuration
            modelBuilder.Entity<QuizParticipant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();

                entity.HasOne(p => p.Session)
                    .WithMany(s => s.Participants)
                    .HasForeignKey(p => p.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Participations)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Answer configuration
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SessionId, e.UserId, e.QuestionId }).IsUnique();

                entity.HasOne(a => a.Session)
                    .WithMany(s => s.Answers)
                    .HasForeignKey(a => a.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}