using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;

namespace GQuiz.Pages.Host
{
    public class SessionResultsModel : PageModel
    {
        private readonly AppDbContext _context;

        public SessionResultsModel(AppDbContext context)
        {
            _context = context;
        }

        public QuizSession? Session { get; set; }
        public List<QuizParticipant> Participants { get; set; } = new();
        public List<QuizParticipant> FlaggedParticipants { get; set; } = new();
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int FlaggedCount { get; set; }
        public double CompletionRate { get; set; }

        public async Task<IActionResult> OnGetAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");

            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            Session = await _context.QuizSessions
                .Include(s => s.Quiz)
                    .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.HostId == userId.Value);

            if (Session == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            Participants = await _context.QuizParticipants
                .Where(p => p.SessionId == sessionId)
                .Include(p => p.User)
                .OrderByDescending(p => p.TotalScore)
                .ToListAsync();

            FlaggedParticipants = Participants.Where(p => p.IsFlagged).ToList();

            if (Participants.Any())
            {
                AverageScore = Participants.Average(p => p.TotalScore);
                HighestScore = Participants.Max(p => p.TotalScore);
                FlaggedCount = FlaggedParticipants.Count;

                var answeredCount = await _context.Answers
                    .Where(a => a.SessionId == sessionId)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                CompletionRate = Participants.Count > 0 ? (answeredCount / (double)Participants.Count) * 100 : 0;
            }

            return Page();
        }
    }
}