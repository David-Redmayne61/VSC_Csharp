using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FirstProject.Data;
using FirstProject.Models;
using FirstProject.Models.ViewModels;

namespace FirstProject.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportGeneratorViewModel();
            
            // Get basic statistics for the dashboard
            var totalCalls = await _context.CustomerContacts.CountAsync();
            var openCalls = await _context.CustomerContacts.CountAsync(c => c.Status == ContactStatus.Open);
            var pendingCalls = await _context.CustomerContacts.CountAsync(c => c.Status == ContactStatus.Pending);
            var closedCalls = await _context.CustomerContacts.CountAsync(c => c.Status == ContactStatus.Closed);

            viewModel.TotalCalls = totalCalls;
            viewModel.OpenCalls = openCalls;
            viewModel.PendingCalls = pendingCalls;
            viewModel.ClosedCalls = closedCalls;

            // Get date range for default values (last 30 days)
            viewModel.StartDate = DateTime.Now.AddDays(-30);
            viewModel.EndDate = DateTime.Now;

            return View(viewModel);
        }

        // GET: Reports/Generate - for quick report links
        [HttpGet]
        public async Task<IActionResult> Generate(DateTime? startDate, DateTime? endDate, ContactStatus? selectedStatus, string? createdBy)
        {
            var model = new ReportGeneratorViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedStatus = selectedStatus,
                CreatedBy = createdBy
            };

            return await GenerateReport(model);
        }

        // POST: Reports/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(ReportGeneratorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            return await GenerateReport(model);
        }

        private async Task<IActionResult> GenerateReport(ReportGeneratorViewModel model)
        {
            var query = _context.CustomerContacts
                .Include(c => c.Person)
                .AsQueryable();

            // Apply filters
            if (model.StartDate.HasValue)
            {
                query = query.Where(c => c.ContactDate >= model.StartDate.Value);
            }

            if (model.EndDate.HasValue)
            {
                query = query.Where(c => c.ContactDate <= model.EndDate.Value.AddDays(1));
            }

            if (model.SelectedStatus.HasValue)
            {
                query = query.Where(c => c.Status == model.SelectedStatus.Value);
            }

            if (!string.IsNullOrEmpty(model.CreatedBy))
            {
                query = query.Where(c => c.CreatedBy != null && c.CreatedBy.Contains(model.CreatedBy));
            }

            var results = await query.OrderByDescending(c => c.ContactDate).ToListAsync();

            var reportData = new ReportDataViewModel
            {
                Filters = model,
                CustomerContacts = results,
                GeneratedDate = DateTime.Now,
                TotalRecords = results.Count
            };

            return View("ReportResults", reportData);
        }

        // GET: Reports/Export
        public async Task<IActionResult> Export(DateTime? startDate, DateTime? endDate, ContactStatus? status, string? createdBy, string format = "csv")
        {
            var query = _context.CustomerContacts
                .Include(c => c.Person)
                .AsQueryable();

            // Apply same filters as Generate
            if (startDate.HasValue)
            {
                query = query.Where(c => c.ContactDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.ContactDate <= endDate.Value.AddDays(1));
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(createdBy))
            {
                query = query.Where(c => c.CreatedBy != null && c.CreatedBy.Contains(createdBy));
            }

            var results = await query.OrderByDescending(c => c.ContactDate).ToListAsync();

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(results);
            }

            return BadRequest("Unsupported export format");
        }

        private IActionResult ExportToCsv(List<CustomerContact> contacts)
        {
            var csv = new System.Text.StringBuilder();
            
            // Add header
            csv.AppendLine("Call Number,Customer Name,Email,Phone,Contact Date,Status,Created By,Last Modified,Modified By,Reason");

            // Add data rows
            foreach (var contact in contacts)
            {
                var customerName = $"{contact.Person?.Forename} {contact.Person?.FamilyName}".Trim();
                var reason = contact.ReasonForContact?.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ");
                
                csv.AppendLine($"\"{contact.CallNumber}\",\"{customerName}\",\"{contact.CustomerEmail}\",\"{contact.CustomerPhone}\"," +
                              $"\"{contact.ContactDate:yyyy-MM-dd HH:mm}\",\"{contact.Status}\"," +
                              $"\"{contact.CreatedBy ?? ""}\",\"{contact.LastModified?.ToString("yyyy-MM-dd HH:mm") ?? ""}\"," +
                              $"\"{contact.ModifiedBy ?? ""}\",\"{reason ?? ""}\"");
            }

            var fileName = $"CustomerCalls_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }
    }
}
