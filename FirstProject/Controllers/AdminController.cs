using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FirstProject.Models;
using FirstProject.Models.ViewModels;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(ResetPasswordViewModel model)
        {
            _logger.LogInformation("ResetUserPassword called with: UserId={UserId}, Username={Username}", 
                model.UserId, model.Username);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("ModelState invalid: {Errors}", errors);
                TempData["ErrorMessage"] = "Invalid password reset data: " + errors;
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null || user.Username.ToLower() == "admin")
            {
                _logger.LogWarning("User not found or attempt to reset admin password. UserId: {UserId}", model.UserId);
                TempData["ErrorMessage"] = "User not found or cannot reset admin password";
                return RedirectToAction(nameof(UserManagement));
            }

            try
            {
                user.PasswordHash = _authService.HashPassword(model.NewPassword);
                user.RequiresPasswordChange = true;  // Ensure this is set
                _logger.LogInformation("Resetting password for user {Username} and setting RequiresPasswordChange flag", user.Username);
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Password reset successful for user {Username}", user.Username);

                TempData["SuccessMessage"] = $"Password reset for user '{user.Username}'. They will be required to change it at next login.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Username}", user.Username);
                TempData["ErrorMessage"] = "An error occurred while resetting the password";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ImportUsers(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to import";
                return RedirectToAction(nameof(UserManagement));
            }

            var result = new ImportResultViewModel();
            var lineNumber = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length != 2)
                    {
                        result.Errors.Add(new ImportError 
                        { 
                            Line = line,
                            Error = "Invalid format - expected Username,Password",
                            LineNumber = lineNumber
                        });
                        continue;
                    }

                    var username = parts[0].Trim();
                    var password = parts[1].Trim();

                    // Check for duplicates
                    if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
                    {
                        result.Duplicates.Add(new DuplicateEntry 
                        { 
                            Username = username,
                            LineNumber = lineNumber,
                            AttemptedImport = DateTime.Now
                        });
                        continue;
                    }

                    // Validate username
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        result.Errors.Add(new ImportError 
                        { 
                            Line = line,
                            Error = "Username cannot be empty",
                            LineNumber = lineNumber
                        });
                        continue;
                    }

                    // Validate password
                    if (password.Length < 8 || password.Length > 12 || 
                        !password.Any(char.IsUpper) || 
                        !password.Any(char.IsLower) || 
                        !password.Any(char.IsDigit))
                    {
                        result.Errors.Add(new ImportError 
                        { 
                            Line = line,
                            Error = "Password does not meet requirements",
                            LineNumber = lineNumber
                        });
                        continue;
                    }

                    try
                    {
                        var user = new User
                        {
                            Username = username,
                            PasswordHash = _authService.HashPassword(password),
                            Created = DateTime.Now
                        };

                        _context.Users.Add(user);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportError 
                        { 
                            Line = line,
                            Error = $"Failed to create user: {ex.Message}",
                            LineNumber = lineNumber
                        });
                    }
                }
            }

            if (result.Errors.Any() || result.Duplicates.Any())
            {
                TempData["ImportResult"] = System.Text.Json.JsonSerializer.Serialize(result);
                return RedirectToAction(nameof(ImportErrors));
            }

            try
            {
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Successfully imported {result.SuccessCount} users";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving changes: {ex.Message}";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        public IActionResult ImportErrors()
        {
            if (TempData["ImportResult"] is string jsonResult)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<ImportResultViewModel>(jsonResult);
                return View(result);
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

        private bool IsCurrentUserAdmin()
        {
            try
            {
                // First try to get username from session if available
                var sessionUsername = HttpContext.Session?.GetString("Username");
                if (!string.IsNullOrEmpty(sessionUsername))
                {
                    return sessionUsername.ToLower() == "admin";
                }
            }
            catch (InvalidOperationException)
            {
                // Session not configured, continue with other methods
            }

            // Fallback to checking User.Identity
            var identityName = User.Identity?.Name;
            if (!string.IsNullOrEmpty(identityName))
            {
                return identityName.ToLower() == "admin";
            }

            // Additional fallback - check if user is authenticated and has admin identity
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.Identity.Name?.ToLower() == "admin";
            }

            return false;
        }

        // Audit Log Management
        [HttpGet]
        public async Task<IActionResult> AuditLogs(
            string? actionFilter = null,
            string? entityType = null,
            string? userName = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 50,
            bool applied = false)
        {
            if (!IsCurrentUserAdmin())
            {
                return Forbid();
            }

            var query = _context.AuditLogs.AsQueryable();

            // Debug: Get total count before any filters
            var totalAuditLogs = await _context.AuditLogs.CountAsync();

            List<AuditLog> auditLogs = new List<AuditLog>();
            int totalRecords = 0;
            int totalPages = 0;

            // Only execute query if filters have been applied
            if (applied)
            {
                // Apply filters
                if (!string.IsNullOrEmpty(actionFilter))
                {
                    query = query.Where(a => a.Action == actionFilter);
                }

                if (!string.IsNullOrEmpty(entityType))
                {
                    query = query.Where(a => a.EntityType == entityType);
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    query = query.Where(a => a.UserName.Contains(userName));
                }

                if (fromDate.HasValue)
                {
                    var fromDateForComparison = fromDate.Value.Date;
                    query = query.Where(a => a.Timestamp.Date >= fromDateForComparison);
                }

                if (toDate.HasValue)
                {
                    var toDateForComparison = toDate.Value.Date;
                    query = query.Where(a => a.Timestamp.Date <= toDateForComparison);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(a => 
                        a.EntityDescription.Contains(searchTerm) ||
                        a.Details.Contains(searchTerm) ||
                        a.OldValues.Contains(searchTerm) ||
                        a.NewValues.Contains(searchTerm));
                }

                totalRecords = await query.CountAsync();
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                auditLogs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            // Debug: Add total count to ViewBag for troubleshooting
            ViewBag.TotalAuditLogsInDatabase = totalAuditLogs;
            ViewBag.FilteredCount = totalRecords;
            ViewBag.FromDateFilter = fromDate;
            ViewBag.ToDateFilter = toDate;

            // Get filter options
            var availableActions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            var availableEntityTypes = await _context.AuditLogs
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            var availableUsers = await _context.AuditLogs
                .Select(a => a.UserName)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            var viewModel = new AuditLogViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                PageSize = pageSize,
                ActionFilter = actionFilter,
                EntityTypeFilter = entityType,
                UserNameFilter = userName,
                FromDate = fromDate,
                ToDate = toDate,
                SearchTerm = searchTerm,
                AvailableActions = availableActions,
                AvailableEntityTypes = availableEntityTypes,
                AvailableUsers = availableUsers
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogDetail(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                return Forbid();
            }

            var auditLog = await _context.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            // Get related logs for the same entity
            var relatedLogs = await _context.AuditLogs
                .Where(a => a.EntityType == auditLog.EntityType && 
                           a.EntityId == auditLog.EntityId && 
                           a.Id != auditLog.Id)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            var viewModel = new AuditLogDetailViewModel
            {
                AuditLog = auditLog,
                RelatedLogs = relatedLogs
            };

            // Parse JSON values if available
            try
            {
                if (!string.IsNullOrEmpty(auditLog.OldValues))
                {
                    viewModel.OldValuesDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(auditLog.OldValues);
                }
                if (!string.IsNullOrEmpty(auditLog.NewValues))
                {
                    viewModel.NewValuesDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(auditLog.NewValues);
                }
            }
            catch
            {
                // If JSON parsing fails, leave as null and display raw values
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOldAuditLogs(int daysToKeep = 90)
        {
            if (!IsCurrentUserAdmin())
            {
                return Forbid();
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var logsToDelete = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .ToListAsync();

            if (logsToDelete.Any())
            {
                _context.AuditLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully deleted {logsToDelete.Count} audit log entries older than {daysToKeep} days.";
            }
            else
            {
                TempData["InfoMessage"] = $"No audit log entries found older than {daysToKeep} days.";
            }

            return RedirectToAction(nameof(AuditLogs));
        }
    }
}