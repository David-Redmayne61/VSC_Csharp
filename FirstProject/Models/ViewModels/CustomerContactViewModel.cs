using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FirstProject.Models.ViewModels
{
    public class CustomerContactViewModel
    {
        [Required]
        [Display(Name = "Call Number")]
        public string CallNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer")]
        public int PersonId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Customer Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Customer Phone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Reason for Contact")]
        [StringLength(5000, ErrorMessage = "Reason for contact cannot exceed 5000 characters")]
        public string ReasonForContact { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contact Date")]
        public DateTime ContactDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Status")]
        public ContactStatus Status { get; set; } = ContactStatus.Open;

        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
        public string? SelectedCustomerName { get; set; }
    }
}
