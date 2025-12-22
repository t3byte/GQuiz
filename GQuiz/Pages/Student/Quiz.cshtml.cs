using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;

namespace GQuiz.Pages.Student
{
    public class QuizModel : PageModel
    {
        private readonly AppDbContext _context;
        public QuizModel(AppDbContext context)
        {
            _context = context;
        }

        public QuizSession? Session { get; set; }
        public int SessionId { get; set; }
        public int UserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            UserId = userId.Value;
            SessionId = sessionId;

            Session = await _context.QuizSessions
                .Include(s => s.Quiz)
                    .ThenInclude(q => q.Questions)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (Session == null)
            {
                return RedirectToPage("/Student/Join");
            }

            // Verify participant
            var participant = await _context.QuizParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId.Value);

            if (participant == null)
            {
                return RedirectToPage("/Student/Join");
            }

            return Page();
        }
    }
}