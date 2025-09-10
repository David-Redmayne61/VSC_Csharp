using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models
{
    public class CustomerInteractionViewModel
    {
        public Person Person { get; set; } = null!;
        public List<CustomerContact> CustomerContacts { get; set; } = new List<CustomerContact>();
        
        [Display(Name = "Total Calls")]
        public int TotalCalls { get; set; }
        
        [Display(Name = "Open Calls")]
        public int OpenCalls { get; set; }
        
        [Display(Name = "Pending Calls")]
        public int PendingCalls { get; set; }
        
        [Display(Name = "Closed Calls")]
        public int ClosedCalls { get; set; }
        
        [Display(Name = "First Contact Date")]
        public DateTime? FirstContactDate { get; set; }
        
        [Display(Name = "Last Contact Date")]
        public DateTime? LastContactDate { get; set; }
    }
}
