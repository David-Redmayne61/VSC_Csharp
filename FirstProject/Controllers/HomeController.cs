using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text; // For StringBuilder
using System.Drawing; // For Color
using OfficeOpenXml; // For ExcelPackage
using FirstProject.Models;
using FirstProject.Data;
using FirstProject.Services;
using Microsoft.AspNetCore.Authorization;

namespace FirstProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService; // Add this field

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            PdfService pdfService) // Add pdfService parameter
        {
            _logger = logger;
            _context = context;
            _pdfService = pdfService;
        }

        // Keep only one Index action method
        public async Task<IActionResult> Index(string forename, string familyName, string gender, int? yearOfBirth)
        {
            var query = _context.People.AsQueryable();

            if (!string.IsNullOrWhiteSpace(forename))
                query = query.Where(p => p.Forename.Contains(forename));

            if (!string.IsNullOrWhiteSpace(familyName))
                query = query.Where(p => p.FamilyName.Contains(familyName));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            if (yearOfBirth.HasValue)
                query = query.Where(p => p.YearOfBirth == yearOfBirth.Value);

            // Add default sorting
            query = query.OrderBy(p => p.FamilyName);

            var people = await query.ToListAsync();
            return View(people);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitNames(string Forename, string FamilyName, string Gender, int YearOfBirth)
        {
            if (string.IsNullOrWhiteSpace(Forename) || string.IsNullOrWhiteSpace(FamilyName) || 
                string.IsNullOrWhiteSpace(Gender) || YearOfBirth < 1900 || YearOfBirth > 2025)
            {
                ViewBag.ErrorMessage = "Please fill all fields correctly!";
                return View("Index");
            }

            // Check for duplicates
            var duplicate = await _context.People
                .AnyAsync(p => p.Forename.ToLower() == Forename.ToLower() && 
                               p.FamilyName.ToLower() == FamilyName.ToLower());

            if (duplicate)
            {
                ViewBag.DuplicateMessage = $"A person with the name {Forename} {FamilyName} already exists.";
                return View("Index");
            }

            var person = new Person
            {
                Forename = Forename,
                FamilyName = FamilyName,
                Gender = Gender,
                YearOfBirth = YearOfBirth
            };

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            ViewBag.SuccessMessage = "Record added successfully!";
            return View("Index");
        }

        public async Task<IActionResult> ViewPeople(string sortOrder)
        {
            ViewBag.IdSortParam = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.ForenameSortParam = sortOrder == "forename" ? "forename_desc" : "forename";
            ViewBag.FamilyNameSortParam = sortOrder == "familyname" ? "familyname_desc" : "familyname";
            ViewBag.GenderSortParam = sortOrder == "gender" ? "gender_desc" : "gender";
            ViewBag.YearOfBirthSortParam = sortOrder == "yearofbirth" ? "yearofbirth_desc" : "yearofbirth";

            var people = from p in _context.People
                         select p;

            switch (sortOrder)
            {
                case "id_desc":
                    people = people.OrderByDescending(p => p.Id);
                    break;
                case "forename":
                    people = people.OrderBy(p => p.Forename);
                    break;
                case "forename_desc":
                    people = people.OrderByDescending(p => p.Forename);
                    break;
                case "familyname":
                    people = people.OrderBy(p => p.FamilyName);
                    break;
                case "familyname_desc":
                    people = people.OrderByDescending(p => p.FamilyName);
                    break;
                case "gender":
                    people = people.OrderBy(p => p.Gender);
                    break;
                case "gender_desc":
                    people = people.OrderByDescending(p => p.Gender);
                    break;
                case "yearofbirth":
                    people = people.OrderBy(p => p.YearOfBirth);
                    break;
                case "yearofbirth_desc":
                    people = people.OrderByDescending(p => p.YearOfBirth);
                    break;
                default:
                    people = people.OrderBy(p => p.FamilyName);  // Changed from Id to FamilyName
                    break;
            }
            return View(await people.ToListAsync());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var person = await _context.People.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Person person)
        {
            if (id != person.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ViewPeople));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.People.AnyAsync(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(person);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var person = await _context.People.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var person = await _context.People.FindAsync(id);
            if (person != null)
            {
                _context.People.Remove(person);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(ViewPeople));
        }

        public async Task<IActionResult> ExportToExcel(string forename, string familyName, string gender, int? yearOfBirth)
        {
            var query = _context.People.AsQueryable();

            if (!string.IsNullOrWhiteSpace(forename))
                query = query.Where(p => p.Forename.Contains(forename));

            if (!string.IsNullOrWhiteSpace(familyName))
                query = query.Where(p => p.FamilyName.Contains(familyName));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            if (yearOfBirth.HasValue)
                query = query.Where(p => p.YearOfBirth == yearOfBirth.Value);

            var people = await query.ToListAsync();
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("People");

            // Add headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Forename";
            worksheet.Cells[1, 3].Value = "Family Name";
            worksheet.Cells[1, 4].Value = "Gender";
            worksheet.Cells[1, 5].Value = "Year of Birth";

            // Style the header row
            var headerRange = worksheet.Cells[1, 1, 1, 5];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

            // Add data
            int row = 2;
            foreach (var person in people)
            {
                worksheet.Cells[row, 1].Value = person.Id;
                worksheet.Cells[row, 2].Value = person.Forename;
                worksheet.Cells[row, 3].Value = person.FamilyName;
                worksheet.Cells[row, 4].Value = person.Gender;
                worksheet.Cells[row, 5].Value = person.YearOfBirth;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return File(
                await package.GetAsByteArrayAsync(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"people_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        public async Task<IActionResult> ExportToCsv()
        {
            var people = await _context.People.ToListAsync();
            
            var builder = new StringBuilder();
            builder.AppendLine("ID,Forename,Family Name,Gender");
            
            foreach (var person in people)
            {
                builder.AppendLine($"{person.Id},{person.Forename},{person.FamilyName},{person.Gender}");
            }

            return File(
                Encoding.UTF8.GetBytes(builder.ToString()),
                "text/csv",
                $"people_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );
        }

        public async Task<IActionResult> Search(string sortOrder, string forename, string familyName, string gender, int? yearOfBirth)
        {
            ViewData["CurrentForename"] = forename;
            ViewData["CurrentFamilyName"] = familyName;
            ViewData["CurrentGender"] = gender;
            ViewData["CurrentYearOfBirth"] = yearOfBirth;

            // Set up sort parameters
            ViewBag.IdSortParam = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.ForenameSortParam = sortOrder == "forename" ? "forename_desc" : "forename";
            ViewBag.FamilyNameSortParam = sortOrder == "familyname" ? "familyname_desc" : "familyname";
            ViewBag.GenderSortParam = sortOrder == "gender" ? "gender_desc" : "gender";
            ViewBag.YearOfBirthSortParam = sortOrder == "yearofbirth" ? "yearofbirth_desc" : "yearofbirth";

            var query = _context.People.AsQueryable();

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(forename))
                query = query.Where(p => p.Forename.Contains(forename));

            if (!string.IsNullOrWhiteSpace(familyName))
                query = query.Where(p => p.FamilyName.Contains(familyName));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            if (yearOfBirth.HasValue)
                query = query.Where(p => p.YearOfBirth == yearOfBirth.Value);

            // Apply sorting
            query = sortOrder switch
            {
                "id_desc" => query.OrderByDescending(p => p.Id),
                "forename" => query.OrderBy(p => p.Forename),
                "forename_desc" => query.OrderByDescending(p => p.Forename),
                "familyname" => query.OrderBy(p => p.FamilyName),
                "familyname_desc" => query.OrderByDescending(p => p.FamilyName),
                "gender" => query.OrderBy(p => p.Gender),
                "gender_desc" => query.OrderByDescending(p => p.Gender),
                "yearofbirth" => query.OrderBy(p => p.YearOfBirth),
                "yearofbirth_desc" => query.OrderByDescending(p => p.YearOfBirth),
                _ => query.OrderBy(p => p.FamilyName)  // Changed from Id to FamilyName
            };

            var results = await query.ToListAsync();
            return View(results);
        }

        public IActionResult ClearSearch()
        {
            return RedirectToAction(nameof(Search));
        }

        public async Task<IActionResult> ExportToPdf(string forename, string familyName, string gender, int? yearOfBirth)
        {
            var query = _context.People.AsQueryable();

            if (!string.IsNullOrWhiteSpace(forename))
                query = query.Where(p => p.Forename.Contains(forename));

            if (!string.IsNullOrWhiteSpace(familyName))
                query = query.Where(p => p.FamilyName.Contains(familyName));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            if (yearOfBirth.HasValue)
                query = query.Where(p => p.YearOfBirth == yearOfBirth.Value);

            var people = await query.ToListAsync();

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                        thead {{ display: table-header-group; }}
                        th, td {{ padding: 8px; border: 1px solid #ddd; text-align: left; }}
                        th {{ background-color: #f8f9fa !important; -webkit-print-color-adjust: exact; }}
                        tr {{ page-break-inside: avoid; }}
                        .footer {{ 
                            position: fixed; 
                            bottom: 0; 
                            width: 100%; 
                            font-size: 12px;
                            border-top: 1px solid #ddd;
                            padding-top: 10px;
                        }}
                        .footer-content {{
                            display: flex;
                            justify-content: space-between;
                            margin: 0 20px;
                        }}
                    </style>
                </head>
                <body>
                    <h2>Database Records</h2>
                    <table>
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Forename</th>
                                <th>Family Name</th>
                                <th>Gender</th>
                                <th>Year of Birth</th>
                            </tr>
                        </thead>
                        <tbody>
                            {string.Join("", people.Select(p => $@"
                                <tr>
                                    <td>{p.Id}</td>
                                    <td>{p.Forename}</td>
                                    <td>{p.FamilyName}</td>
                                    <td>{p.Gender}</td>
                                    <td>{p.YearOfBirth}</td>
                                </tr>
                            "))}
                        </tbody>
                    </table>
                    <div class='footer'>
                        <div class='footer-content'>
                            <span>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</span>
                            <span>First Project</span>
                            <span>Page [page] of [topage]</span>
                        </div>
                    </div>
                </body>
                </html>";

            var pdf = _pdfService.GeneratePdf(html);
            return File(pdf, "application/pdf", $"export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpGet]
        [Authorize]  // This ensures only authenticated users can access
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to import";
                return View();
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                int successCount = 0;
                int duplicateCount = 0;
                var duplicates = new List<string>();
                var otherErrors = new List<string>();

                // Start from row 2 (skip header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var forename = worksheet.Cells[row, 1].Text?.Trim();
                        var familyName = worksheet.Cells[row, 2].Text?.Trim();
                        var gender = worksheet.Cells[row, 3].Text?.Trim();
                        var yearOfBirthText = worksheet.Cells[row, 4].Text?.Trim();

                        if (string.IsNullOrEmpty(forename) || string.IsNullOrEmpty(familyName))
                        {
                            otherErrors.Add($"Row {row}: Forename and Family Name are required");
                            continue;
                        }

                        if (!int.TryParse(yearOfBirthText, out int yearOfBirth))
                        {
                            otherErrors.Add($"Row {row}: Invalid Year of Birth");
                            continue;
                        }

                        // Check for duplicates
                        var isDuplicate = await _context.People.AnyAsync(p => 
                            p.Forename.ToLower() == forename.ToLower() && 
                            p.FamilyName.ToLower() == familyName.ToLower());

                        if (isDuplicate)
                        {
                            duplicateCount++;
                            duplicates.Add($"{forename} {familyName}");
                            continue;
                        }

                        var person = new Person
                        {
                            Forename = forename,
                            FamilyName = familyName,
                            Gender = gender ?? "",
                            YearOfBirth = yearOfBirth
                        };

                        _context.People.Add(person);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        otherErrors.Add($"Row {row}: {ex.Message}");
                    }
                }

                if (successCount > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Successfully imported {successCount} records.";
                }

                if (duplicateCount > 0)
                {
                    var duplicateMessage = $"{duplicateCount} duplicate entries found:\n";
                    if (duplicates.Count > 5)
                    {
                        // Show first 5 duplicates only
                        duplicateMessage += string.Join(", ", duplicates.Take(5));
                        duplicateMessage += $", and {duplicateCount - 5} more...";
                    }
                    else
                    {
                        duplicateMessage += string.Join(", ", duplicates);
                    }
                    TempData["WarningMessage"] = duplicateMessage;
                }

                if (otherErrors.Any())
                {
                    TempData["ErrorMessage"] = $"Errors during import:\n{string.Join("\n", otherErrors)}";
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Import failed: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteMultiple()
        {
            var people = await _context.People
                .OrderBy(p => p.FamilyName)
                .ThenBy(p => p.Forename)
                .ToListAsync();

            var viewModel = new PeopleViewModel
            {
                People = people.Select(p => new PersonViewModel { Person = p }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(PeopleViewModel model)
        {
            if (model?.People == null || !model.People.Any(p => p.IsSelected))
            {
                TempData["ErrorMessage"] = "No records were selected for deletion.";
                return RedirectToAction(nameof(DeleteMultiple));
            }

            var selectedIds = model.People
                .Where(p => p.IsSelected)
                .Select(p => p.Person.Id)
                .ToList();

            var peopleToDelete = await _context.People
                .Where(p => selectedIds.Contains(p.Id))
                .ToListAsync();

            if (peopleToDelete.Any())
            {
                _context.People.RemoveRange(peopleToDelete);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully deleted {peopleToDelete.Count} records.";
            }

            return RedirectToAction(nameof(DeleteMultiple));
        }
    }
}
