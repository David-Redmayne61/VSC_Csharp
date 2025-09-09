using System.ComponentModel.DataAnnotations;
using FirstProject.Models;

namespace FirstProject.Models.ViewModels
{
    public class ReportGeneratorViewModel
    {
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Status Filter")]
        public ContactStatus? SelectedStatus { get; set; }

        [Display(Name = "Created By (contains)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Report Format")]
        public string ReportFormat { get; set; } = "table";

        // Summary statistics for dashboard
        public int TotalCalls { get; set; }
        public int OpenCalls { get; set; }
        public int PendingCalls { get; set; }
        public int ClosedCalls { get; set; }
    }

    public class ReportDataViewModel
    {
        public ReportGeneratorViewModel Filters { get; set; } = new();
        public List<CustomerContact> CustomerContacts { get; set; } = new();
        public DateTime GeneratedDate { get; set; }
        public int TotalRecords { get; set; }

        // Additional statistics
        public int OpenCallsInResults => CustomerContacts.Count(c => c.Status == ContactStatus.Open);
        public int PendingCallsInResults => CustomerContacts.Count(c => c.Status == ContactStatus.Pending);
        public int ClosedCallsInResults => CustomerContacts.Count(c => c.Status == ContactStatus.Closed);

        // Date range statistics
        public DateTime? EarliestCall => CustomerContacts.Any() ? CustomerContacts.Min(c => c.ContactDate) : null;
        public DateTime? LatestCall => CustomerContacts.Any() ? CustomerContacts.Max(c => c.ContactDate) : null;

        // Top contributors
        public List<string> TopContributors => CustomerContacts
            .Where(c => !string.IsNullOrEmpty(c.CreatedBy))
            .GroupBy(c => c.CreatedBy)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()} calls)")
            .ToList();
    }
}
