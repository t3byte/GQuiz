using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;

namespace GQuiz.Pages.Host
{
    public class StartSessionModel : PageModel
    {
        private readonly AppDbContext _context;

        public StartSessionModel(AppDbContext context)
        {
            _context = context;
        }

        public Quiz? Quiz { get; set; }
        public int QuizId { get; set; }
        public string? SessionCode { get; set; }
        public int? SessionId { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int quizId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");
            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            QuizId = quizId;
            Quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.CreatedByUserId == userId.Value);

            if (Quiz == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int quizId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            Quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.CreatedByUserId == userId.Value);

            if (Quiz == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            QuizId = quizId;

            try
            {
                // Generate unique 6-digit session code
                string code;
                do
                {
                    code = new Random().Next(100000, 999999).ToString();
                } while (await _context.QuizSessions.AnyAsync(s => s.SessionCode == code));

                var session = new QuizSession
                {
                    QuizId = quizId,
                    HostId = userId.Value,
                    SessionCode = code,
                    Status = SessionStatus.NotStarted
                };

                _context.QuizSessions.Add(session);
                await _context.SaveChangesAsync();

                SessionCode = code;
                SessionId = session.Id;

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error creating session: " + ex.Message;
                return Page();
            }
        }
    }
}