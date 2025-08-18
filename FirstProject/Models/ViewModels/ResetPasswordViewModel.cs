using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 12 characters")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}