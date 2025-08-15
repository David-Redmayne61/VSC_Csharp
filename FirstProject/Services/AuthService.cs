using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using FirstProject.Models;
using FirstProject.Data;

namespace FirstProject.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null) return null;

            bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return validPassword ? user : null;
        }

        public string HashPassword(string password)
        {
            // For demo purposes, using a simple hash. In production, use a proper password hashing algorithm
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8 || password.Length > 12) return false;

            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasLowerChar = new Regex(@"[a-z]+");

            return hasNumber.IsMatch(password) && 
                   hasUpperChar.IsMatch(password) && 
                   hasLowerChar.IsMatch(password);
        }
    }
}