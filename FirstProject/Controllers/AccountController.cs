using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FirstProject.Models;
using FirstProject.Models.ViewModels;
using FirstProject.Services;
using FirstProject.Data;
using Microsoft.Extensions.Logging;

namespace FirstProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.ValidateUserAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("RequiresPasswordChange", user.RequiresPasswordChange.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (user.RequiresPasswordChange)
            {
                _logger.LogInformation("User {Username} requires password change, redirecting", user.Username);
                TempData["WarningMessage"] = "You must change your password before continuing.";
                return RedirectToAction(nameof(ChangePassword));
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            // Update password
            user.PasswordHash = _authService.HashPassword(model.NewPassword);
            user.RequiresPasswordChange = false;  // Clear the flag
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {Username}", user.Username);
            TempData["SuccessMessage"] = "Password changed successfully";

            // If this was a forced password change, redirect to original destination or home
            if (User.HasClaim(c => c.Type == "RequiresPasswordChange" && c.Value == "True"))
            {
                var identity = (ClaimsIdentity)User.Identity!;
                var claim = identity.FindFirst("RequiresPasswordChange");
                if (claim != null)
                    identity.RemoveClaim(claim);
                identity.AddClaim(new Claim("RequiresPasswordChange", "False"));
                
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ChangeAdminPassword()
        {
            if (User.Identity?.Name?.ToLower() != "admin")
                return RedirectToAction("Index", "Home");

            return View("ChangePassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAdminPassword(ChangePasswordViewModel model)
        {
            if (User.Identity?.Name?.ToLower() != "admin")
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View("ChangePassword", model);

            var admin = await _context.Users.FirstOrDefaultAsync(u => 
                u.Username.ToLower() == "admin");

            if (admin == null)
            {
                ModelState.AddModelError("", "Admin user not found");
                return View("ChangePassword", model);
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, admin.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View("ChangePassword", model);
            }

            // Update password
            admin.PasswordHash = _authService.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin password changed successfully";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid password reset data";
                return RedirectToAction("UserManagement", "Admin");  // Specify controller name
            }

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null || user.Username.ToLower() == "admin")
            {
                TempData["ErrorMessage"] = "User not found or cannot reset admin password";
                return RedirectToAction("UserManagement", "Admin");  // Specify controller name
            }

            user.PasswordHash = _authService.HashPassword(model.NewPassword);
            user.RequiresPasswordChange = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Password reset for user '{user.Username}'. They will be required to change it at next login.";
            return RedirectToAction("UserManagement", "Admin");  // Specify controller name
        }
    }
}