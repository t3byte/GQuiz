using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GQuiz.Data;
using GQuiz.Models;
using System.Text.Json;

namespace GQuiz.Pages.Host
{
    public class EditQuizModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditQuizModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public List<object>? InitialQuestions { get; set; }

        public class InputModel
        {
            public int QuizId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string QuestionsJson { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");
            if (userId == null || isHost != "true")
            {
                return RedirectToPage("/Login");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id && q.CreatedByUserId == userId.Value);

            if (quiz == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            Input.QuizId = quiz.Id;
            Input.Title = quiz.Title;
            Input.Description = quiz.Description;
            InitialQuestions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new {
                    q.Text, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer, q.TimeLimit, q.Points
                }).ToList<object>();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == Input.QuizId && q.CreatedByUserId == userId.Value);

            if (quiz == null)
            {
                return RedirectToPage("/Host/Dashboard");
            }

            // Update quiz
            quiz.Title = Input.Title;
            quiz.Description = Input.Description;

            // Replace questions: simple approach - remove existing and add new
            var existing = quiz.Questions.ToList();
            _context.Questions.RemoveRange(existing);

            var questions = JsonSerializer.Deserialize<List<QuestionDto>>(Input.QuestionsJson) ?? new List<QuestionDto>();
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var question = new Question
                {
                    QuizId = quiz.Id,
                    Text = q.Text,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer,
                    TimeLimit = q.TimeLimit,
                    Points = q.Points,
                    OrderIndex = i
                };
                _context.Questions.Add(question);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/Host/Dashboard");
        }

        public class QuestionDto
        {
            public string Text { get; set; } = string.Empty;
            public string OptionA { get; set; } = string.Empty;
            public string OptionB { get; set; } = string.Empty;
            public string OptionC { get; set; } = string.Empty;
            public string OptionD { get; set; } = string.Empty;
            public string CorrectAnswer { get; set; } = string.Empty;
            public int TimeLimit { get; set; }
            public int Points { get; set; }
            public int OrderIndex { get; set; }
        }
    }
}
