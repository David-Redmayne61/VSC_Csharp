using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using FirstProject.Models;
using FirstProject.Data;
using System.Threading.Tasks;

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
            ViewData["Message"] = "David Redmayne"; // Add this line
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitNames(string Forename, string FamilyName)
        {
            if (string.IsNullOrWhiteSpace(Forename) || string.IsNullOrWhiteSpace(FamilyName))
            {
                ViewData["ErrorMessage"] = "You cannot have blank fields!";
                return View("Index");
            }

            var person = new Person
            {
                Forename = Forename,
                FamilyName = FamilyName
            };

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            ViewData["Message"] = $"Hello {Forename} {FamilyName}";
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
