using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GQuiz.Data;
using GQuiz.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GQuiz.Pages.Host
{
    public class CreateQuizModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateQuizModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;

            [StringLength(500)]
            public string Description { get; set; } = string.Empty;

            public string QuestionsJson { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var isHost = HttpContext.Session.GetString("IsHost");

            if (userId == null || isHost != "true")
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

            try
            {
                var questions = JsonSerializer.Deserialize<List<QuestionDto>>(Input.QuestionsJson);

                if (questions == null || questions.Count == 0)
                {
                    ErrorMessage = "Please add at least one question";
                    return Page();
                }

                var quiz = new Quiz
                {
                    Title = Input.Title,
                    Description = Input.Description,
                    CreatedByUserId = userId.Value
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                foreach (var q in questions)
                {
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
                        OrderIndex = q.OrderIndex
                    };
                    _context.Questions.Add(question);
                }

                await _context.SaveChangesAsync();

                return RedirectToPage("/Host/Dashboard");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error creating quiz: " + ex.Message;
                return Page();
            }
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