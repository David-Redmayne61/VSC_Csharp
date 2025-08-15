using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 12 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}