using System.ComponentModel.DataAnnotations;

namespace FirstProject.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [StringLength(200)]
        public string EntityDescription { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(2000)]
        public string Details { get; set; } = string.Empty;

        [StringLength(8000)]
        public string OldValues { get; set; } = string.Empty;

        [StringLength(8000)]
        public string NewValues { get; set; } = string.Empty;
    }

    public static class AuditActions
    {
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string View = "VIEW";
        public const string Login = "LOGIN";
        public const string Logout = "LOGOUT";
        public const string Failed_Login = "FAILED_LOGIN";
    }

    public static class EntityTypes
    {
        public const string CustomerContact = "CustomerContact";
        public const string Person = "Person";
        public const string User = "User";
        public const string System = "System";
    }
}
