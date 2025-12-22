using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;
using System.ComponentModel.DataAnnotations;
using GQuiz.Services;

namespace GQuiz.Pages.Student
{
    public class JoinModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly QuizSessionManager _sessionManager;

        public JoinModel(AppDbContext context, QuizSessionManager sessionManager)
        {
            _context = context;
            _sessionManager = sessionManager;
        }

        [BindProperty]
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string SessionCode { get; set; } = string.Empty;

        public QuizSession? Session { get; set; }
        public int? SessionId { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Session = await _context.QuizSessions
                .Include(s => s.Quiz)
                    .ThenInclude(q => q.Questions)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.SessionCode == SessionCode);

            if (Session == null)
            {
                ErrorMessage = "Invalid session code. Please check and try again.";
                return Page();
            }

            if (Session.Status == SessionStatus.Completed)
            {
                ErrorMessage = "This session has already ended.";
                return Page();
            }

            // Allow joining whether session is NotStarted or InProgress
            SessionId = Session.Id;
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync(int sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Check if already joined
            var existing = await _context.QuizParticipants
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId.Value);

            if (existing == null)
            {
                var participant = new QuizParticipant
                {
                    SessionId = sessionId,
                    UserId = userId.Value,
                    TotalScore = 0,
                    TabSwitchCount = 0,
                    IsFlagged = false
                };

                _context.QuizParticipants.Add(participant);
                await _context.SaveChangesAsync();

                // Ensure in-memory session state knows about this participant so scoring and tab tracking work
                var state = _sessionManager.GetSession(sessionId);
                if (state != null)
                {
                    state.ParticipantScores.TryAdd(userId.Value, 0);
                    state.TabSwitchCounts.TryAdd(userId.Value, 0);
                }
            }

            return RedirectToPage("/Student/Quiz", new { sessionId });
        }
    }
}