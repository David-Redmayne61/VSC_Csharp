using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models
{
    public class Person
    {
        public int Id { get; set; }
        
        [Required]
        public required string Forename { get; set; }
        
        [Required]
        public required string FamilyName { get; set; }

        [Required]
        public required string Gender { get; set; }

        [Required]
        [Range(1900, 2025, ErrorMessage = "Please enter a valid year between 1900 and 2025")]
        [Display(Name = "Year of Birth")]
        public int YearOfBirth { get; set; }
    }
}