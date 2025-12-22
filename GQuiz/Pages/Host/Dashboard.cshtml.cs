using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;

namespace GQuiz.Pages.Host
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Quiz> Quizzes { get; set; } = new();
        public List<QuizSession> RecentSessions { get; set; } = new();
        public int TotalQuizzes { get; set; }
        public int ActiveSessions { get; set; }
        public int TotalParticipants { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");

            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            Quizzes = await _context.Quizzes
                .Where(q => q.CreatedByUserId == userId.Value)
                .Include(q => q.Questions)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            RecentSessions = await _context.QuizSessions
                .Where(s => s.HostId == userId.Value)
                .Include(s => s.Quiz)
                .Include(s => s.Participants)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            TotalQuizzes = Quizzes.Count;
            ActiveSessions = RecentSessions.Count(s => s.Status == SessionStatus.InProgress);
            TotalParticipants = await _context.QuizParticipants
                .Where(p => p.Session.HostId == userId.Value)
                .CountAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");

            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            var session = await _context.QuizSessions.FindAsync(sessionId);
            if (session == null || session.HostId != userId.Value)
            {
                return Forbid();
            }

            if (session.Status != SessionStatus.NotStarted)
            {
                // Only allow deleting sessions that haven't started
                return BadRequest();
            }

            _context.QuizSessions.Remove(session);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}