using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstProject.Models
{
    public class CustomerContact
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Call Number")]
        public required string CallNumber { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int PersonId { get; set; }

        [ForeignKey("PersonId")]
        public virtual Person? Person { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Customer Email")]
        public required string CustomerEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Customer Phone")]
        public required string CustomerPhone { get; set; }

        [Required]
        [Display(Name = "Reason for Contact")]
        [StringLength(5000, ErrorMessage = "Reason for contact cannot exceed 5000 characters")]
        public required string ReasonForContact { get; set; }

        [Required]
        [Display(Name = "Contact Date")]
        public DateTime ContactDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ContactStatus Status { get; set; } = ContactStatus.Open;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModified { get; set; }

        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }
    }
}
