using FirstProject.Models;

namespace FirstProject.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string username, string password);
        string HashPassword(string password);
        bool ValidatePassword(string password);
    }
}