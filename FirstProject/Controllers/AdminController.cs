using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FirstProject.Models;
using FirstProject.Services;
using FirstProject.Data;

namespace FirstProject.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context, 
            IAuthService authService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> UserManagement()
        {
            return View(await GetUserManagementViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(FirstProject.Models.ViewModels.CreateUserViewModel model)
        {
            _logger.LogDebug("Create User action started for username: {Username}", model.Username);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                    }
                    return View("UserManagement", await GetUserManagementViewModel(model));
                }

                var existingUser = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());

                if (existingUser)
                {
                    _logger.LogWarning("Username already exists: {Username}", model.Username);
                    ModelState.AddModelError("Username", "Username already exists");
                    return View("UserManagement", await GetUserManagementViewModel(model));
                }

                var user = new User
                {
                    Username = model.Username.Trim(),
                    PasswordHash = _authService.HashPassword(model.Password),
                    Created = DateTime.Now
                };

                _logger.LogDebug("Adding new user to context");
                _context.Users.Add(user);
                
                _logger.LogDebug("Saving changes to database");
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges result: {SaveResult} rows affected", saveResult);

                if (saveResult > 0)
                {
                    _logger.LogInformation("User created successfully: {Username}", user.Username);
                    TempData["SuccessMessage"] = $"User '{user.Username}' created successfully";
                    return RedirectToAction(nameof(UserManagement));
                }
                else
                {
                    _logger.LogError("SaveChanges returned 0 rows affected");
                    ModelState.AddModelError("", "Failed to create user - no rows affected");
                    return View("UserManagement", await GetUserManagementViewModel(model));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", model.Username);
                ModelState.AddModelError("", "An error occurred while creating the user");
                return View("UserManagement", await GetUserManagementViewModel(model));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction(nameof(UserManagement));
            }

            // Prevent deletion of admin account
            if (user.Username.ToLower() == "admin")
            {
                TempData["ErrorMessage"] = "Cannot delete admin account";
                return RedirectToAction(nameof(UserManagement));
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User '{user.Username}' deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Username}", user.Username);
                TempData["ErrorMessage"] = "Error deleting user";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        private async Task<UserManagementViewModel> GetUserManagementViewModel(FirstProject.Models.ViewModels.CreateUserViewModel? newUser = null)
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} users from database", users.Count);
            
            return new UserManagementViewModel
            {
                Users = users,
                NewUser = newUser ?? new FirstProject.Models.ViewModels.CreateUserViewModel()
            };
        }
    }
}