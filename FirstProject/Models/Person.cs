using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models
{
    public class Person
    {
        public int Id { get; set; }
        
        [Required]
        public string Forename { get; set; }
        
        [Required]
        public string FamilyName { get; set; }
    }
}