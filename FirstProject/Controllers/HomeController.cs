using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using FirstProject.Models;
using FirstProject.Data;
using System.Threading.Tasks;
using System.Text;
using OfficeOpenXml;
using System.Drawing;

namespace FirstProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
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
                    people = people.OrderBy(p => p.Id);
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

        public async Task<IActionResult> ExportToExcel()
        {
            var people = await _context.People.ToListAsync();
            
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
