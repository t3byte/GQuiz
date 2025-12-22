using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GQuiz.Services;
using System.ComponentModel.DataAnnotations;

namespace GQuiz.Pages
{
    public class SignupModel : PageModel
    {
        private readonly AuthService _authService;

        public SignupModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;

            public bool IsHost { get; set; }
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

            // Use bound property
            var isHost = Input.IsHost;

            var user = await _authService.RegisterAsync(
                Input.Username,
                Input.Email,
                Input.Password,
                isHost
            );

            if (user == null)
            {
                ErrorMessage = "Username or email already exists";
                return Page();
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("IsHost", user.IsHost.ToString());

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