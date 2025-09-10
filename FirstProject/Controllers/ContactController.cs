using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FirstProject.Data;
using FirstProject.Models;
using FirstProject.Models.ViewModels;
using FirstProject.Services;
using Microsoft.AspNetCore.Authorization;

namespace FirstProject.Controllers
{
    [Authorize]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ContactController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: Contact
        public async Task<IActionResult> Index()
        {
            var contacts = await _context.CustomerContacts
                .Include(c => c.Person)
                .OrderByDescending(c => c.ContactDate)
                .ToListAsync();

            return View(contacts);
        }

        // GET: Contact/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CustomerContactViewModel();
            await PopulateCustomersDropdown(viewModel);
            
            // Generate unique call number
            viewModel.CallNumber = await GenerateCallNumber();
            
            return View(viewModel);
        }

        // POST: Contact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if call number is unique
                var existingContact = await _context.CustomerContacts
                    .FirstOrDefaultAsync(c => c.CallNumber == model.CallNumber);
                
                if (existingContact != null)
                {
                    ModelState.AddModelError("CallNumber", "This call number already exists. Please use a different number.");
                    await PopulateCustomersDropdown(model);
                    return View(model);
                }

                var contact = new CustomerContact
                {
                    CallNumber = model.CallNumber,
                    PersonId = model.PersonId,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    ReasonForContact = model.ReasonForContact,
                    ContactDate = model.ContactDate,
                    Status = model.Status,
                    CreatedBy = User.Identity?.Name,
                    LastModified = DateTime.Now,
                    ModifiedBy = User.Identity?.Name
                };

                _context.CustomerContacts.Add(contact);
                await _context.SaveChangesAsync();

                // Log the creation action
                var newValues = new
                {
                    contact.Id,
                    contact.CallNumber,
                    contact.PersonId,
                    contact.CustomerEmail,
                    contact.CustomerPhone,
                    contact.ContactDate,
                    contact.Status,
                    contact.ReasonForContact
                };

                await _auditService.LogAsync(
                    AuditActions.Create,
                    EntityTypes.CustomerContact,
                    contact.Id,
                    $"Customer Call #{contact.CallNumber}",
                    GetCurrentUsername(),
                    HttpContext,
                    null,
                    newValues.ToAuditString(),
                    "New customer contact created via web interface"
                );

                TempData["SuccessMessage"] = "Customer contact recorded successfully!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCustomersDropdown(model);
            return View(model);
        }

        // GET: Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.CustomerContacts
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contact/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.CustomerContacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            // Prepare content for editing - add a new divider and position for new content
            var reasonForEdit = contact.ReasonForContact;
            if (!string.IsNullOrEmpty(reasonForEdit))
            {
                // Add a new divider for the upcoming edit
                var newEditDivider = $"<hr style='border: 2px dashed #6c757d; margin: 15px 0;'>" +
                                    $"<p style='color: #6c757d; font-size: 12px; margin: 10px 0;'>" +
                                    $"<strong>Editing on {DateTime.Now:dd/MM/yyyy 'at' HH:mm} by {User.Identity?.Name ?? "Unknown"}</strong></p>";
                
                reasonForEdit = reasonForEdit + newEditDivider + "<p></p>"; // Add empty paragraph for cursor positioning
            }

            var viewModel = new CustomerContactViewModel
            {
                CallNumber = contact.CallNumber,
                PersonId = contact.PersonId,
                CustomerEmail = contact.CustomerEmail,
                CustomerPhone = contact.CustomerPhone,
                ReasonForContact = reasonForEdit,
                ContactDate = contact.ContactDate,
                Status = contact.Status
            };

            await PopulateCustomersDropdown(viewModel);
            return View(viewModel);
        }

        // POST: Contact/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var contact = await _context.CustomerContacts.FindAsync(id);
                    if (contact == null)
                    {
                        return NotFound();
                    }

                    // Check if call number is unique (excluding current record)
                    var existingContact = await _context.CustomerContacts
                        .FirstOrDefaultAsync(c => c.CallNumber == model.CallNumber && c.Id != id);
                    
                    if (existingContact != null)
                    {
                        ModelState.AddModelError("CallNumber", "This call number already exists. Please use a different number.");
                        await PopulateCustomersDropdown(model);
                        return View(model);
                    }

                    // Capture old values for audit logging
                    var oldValues = new
                    {
                        contact.CallNumber,
                        contact.PersonId,
                        contact.CustomerEmail,
                        contact.CustomerPhone,
                        contact.ContactDate,
                        contact.Status,
                        contact.ReasonForContact
                    };

                    contact.CallNumber = model.CallNumber;
                    contact.PersonId = model.PersonId;
                    contact.CustomerEmail = model.CustomerEmail;
                    contact.CustomerPhone = model.CustomerPhone;
                    contact.ContactDate = model.ContactDate;
                    contact.Status = model.Status;

                    // Simply save the edited content as-is (it already includes the audit trail)
                    contact.ReasonForContact = model.ReasonForContact;

                    // Set audit fields for tracking changes
                    contact.LastModified = DateTime.Now;
                    contact.ModifiedBy = User.Identity?.Name ?? "System";

                    // Capture new values for audit logging
                    var newValues = new
                    {
                        contact.CallNumber,
                        contact.PersonId,
                        contact.CustomerEmail,
                        contact.CustomerPhone,
                        contact.ContactDate,
                        contact.Status,
                        contact.ReasonForContact
                    };

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    // Log the update action
                    await _auditService.LogAsync(
                        AuditActions.Update,
                        EntityTypes.CustomerContact,
                        contact.Id,
                        $"Customer Call #{contact.CallNumber}",
                        GetCurrentUsername(),
                        HttpContext,
                        oldValues.ToAuditString(),
                        newValues.ToAuditString(),
                        "Customer contact updated via web interface"
                    );

                    TempData["SuccessMessage"] = "Customer contact updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            await PopulateCustomersDropdown(model);
            return View(model);
        }

        // GET: Contact/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.CustomerContacts
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.CustomerContacts
                .Include(c => c.Person)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (contact != null)
            {
                // Capture values for audit logging before deletion
                var deletedValues = new
                {
                    contact.Id,
                    contact.CallNumber,
                    contact.PersonId,
                    PersonName = $"{contact.Person?.Forename} {contact.Person?.FamilyName}",
                    contact.CustomerEmail,
                    contact.CustomerPhone,
                    contact.ContactDate,
                    contact.Status,
                    contact.ReasonForContact,
                    contact.CreatedBy,
                    contact.LastModified,
                    contact.ModifiedBy
                };

                _context.CustomerContacts.Remove(contact);
                await _context.SaveChangesAsync();

                // Log the deletion action
                await _auditService.LogAsync(
                    AuditActions.Delete,
                    EntityTypes.CustomerContact,
                    contact.Id,
                    $"Customer Call #{contact.CallNumber} - {contact.Person?.Forename} {contact.Person?.FamilyName}",
                    GetCurrentUsername(),
                    HttpContext,
                    deletedValues.ToAuditString(),
                    null,
                    $"Customer contact record permanently deleted from system. Call Number: {contact.CallNumber}"
                );

                TempData["SuccessMessage"] = "Customer contact deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCustomersDropdown(CustomerContactViewModel model)
        {
            var customers = await _context.People
                .OrderBy(p => p.FamilyName)
                .ThenBy(p => p.Forename)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.FamilyName}, {p.Forename}"
                })
                .ToListAsync();

            model.Customers = customers;
        }

        private string GetCurrentUsername()
        {
            try
            {
                // First try to get username from session if available
                var sessionUsername = HttpContext.Session?.GetString("Username");
                if (!string.IsNullOrEmpty(sessionUsername))
                {
                    return sessionUsername;
                }
            }
            catch (InvalidOperationException)
            {
                // Session not configured, continue with other methods
            }

            // Fallback to User.Identity
            return User.Identity?.Name ?? "Unknown";
        }

        private async Task<string> GenerateCallNumber()
        {
            var today = DateTime.Today.ToString("yyyyMMdd");
            var todayContacts = await _context.CustomerContacts
                .Where(c => c.CallNumber.StartsWith(today))
                .CountAsync();

            return $"{today}-{(todayContacts + 1):D3}";
        }
    }
}
