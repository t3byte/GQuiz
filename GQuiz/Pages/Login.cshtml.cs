using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GQuiz.Services;
using System.ComponentModel.DataAnnotations;

namespace GQuiz.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Identifier { get; set; } = string.Empty; // email or username

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _authService.LoginAsync(Input.Identifier, Input.Password);

            if (user == null)
            {
                ErrorMessage = "Invalid email/username or password";
                return Page();
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("IsHost", user.IsHost.ToString().ToLowerInvariant());

            // Redirect based on role
            if (user.IsHost)
            {
                return RedirectToPage("/Host/Dashboard");
            }
            else
            {
                return RedirectToPage("/Student/Join");
            }
        }
    }
}