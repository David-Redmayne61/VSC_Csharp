using FirstProject.Models;

namespace FirstProject.Models.ViewModels
{
    public class AuditLogViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 50;
        
        // Filter properties
        public string? ActionFilter { get; set; }
        public string? EntityTypeFilter { get; set; }
        public string? UserNameFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        
        // Available filter options
        public List<string> AvailableActions { get; set; } = new List<string>();
        public List<string> AvailableEntityTypes { get; set; } = new List<string>();
        public List<string> AvailableUsers { get; set; } = new List<string>();
    }

    public class AuditLogDetailViewModel
    {
        public AuditLog AuditLog { get; set; } = new AuditLog();
        public Dictionary<string, object>? OldValuesDict { get; set; }
        public Dictionary<string, object>? NewValuesDict { get; set; }
        public List<AuditLog> RelatedLogs { get; set; } = new List<AuditLog>();
    }
}
