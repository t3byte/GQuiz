using GQuiz.Data;
using GQuiz.Models;
using Microsoft.EntityFrameworkCore;

namespace GQuiz.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> RegisterAsync(string username, string email, string password, bool isHost = false)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
            {
                return null;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsHost = isHost
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        // Allow login by email OR username
        public async Task<User?> LoginAsync(string identifier, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }

            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}