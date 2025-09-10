using FirstProject.Models;
using FirstProject.Data;
using System.Text.Json;

namespace FirstProject.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, int? entityId, string entityDescription, 
                     string userName, string? oldValues = null, string? newValues = null, string? details = null);
        Task LogAsync(string action, string entityType, int? entityId, string entityDescription, 
                     string userName, HttpContext httpContext, string? oldValues = null, string? newValues = null, string? details = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string entityType, int? entityId, string entityDescription, 
                                  string userName, string? oldValues = null, string? newValues = null, string? details = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityDescription = entityDescription,
                UserName = userName,
                Details = details ?? string.Empty,
                OldValues = oldValues ?? string.Empty,
                NewValues = newValues ?? string.Empty,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogAsync(string action, string entityType, int? entityId, string entityDescription, 
                                  string userName, HttpContext httpContext, string? oldValues = null, string? newValues = null, string? details = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityDescription = entityDescription,
                UserName = userName,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                Details = details ?? string.Empty,
                OldValues = oldValues ?? string.Empty,
                NewValues = newValues ?? string.Empty,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private string GetClientIpAddress(HttpContext context)
        {
            try
            {
                // Try to get IP from X-Forwarded-For header (for reverse proxies)
                var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xForwardedFor))
                {
                    return xForwardedFor.Split(',')[0].Trim();
                }

                // Try to get IP from X-Real-IP header
                var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xRealIp))
                {
                    return xRealIp;
                }

                // Fall back to RemoteIpAddress
                return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public static class AuditExtensions
    {
        public static string ToAuditString(this object obj)
        {
            if (obj == null) return string.Empty;
            
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return obj.ToString() ?? string.Empty;
            }
        }
    }
}
