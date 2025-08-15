using FirstProject.Models;
using FirstProject.Services;

namespace FirstProject.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, IAuthService authService)  // Changed from AuthService to IAuthService
        {
            context.Database.EnsureCreated();

            // Check if we already have users
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = authService.HashPassword("Admin123!")
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}