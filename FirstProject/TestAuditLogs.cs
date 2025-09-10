using Microsoft.EntityFrameworkCore;
using FirstProject.Data;

namespace FirstProject
{
    public class TestAuditLogs
    {
        public static async Task CheckAuditLogs()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FirstProject;Trusted_Connection=true;MultipleActiveResultSets=true")
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var auditLogs = await context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
                
            Console.WriteLine($"Total audit logs in database: {auditLogs.Count}");
            
            foreach (var log in auditLogs)
            {
                Console.WriteLine($"ID: {log.Id}, Timestamp: {log.Timestamp:yyyy-MM-dd HH:mm:ss} UTC (Kind: {log.Timestamp.Kind}), Action: {log.Action}, User: {log.UserName}, Entity: {log.EntityType}");
            }
        }
    }
}
