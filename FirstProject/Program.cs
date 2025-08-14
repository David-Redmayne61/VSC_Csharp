using Microsoft.EntityFrameworkCore;
using FirstProject.Data;
using OfficeOpenXml;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;
using FirstProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure EPPlus license
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

// Configure PDF converter
var architecture = Environment.Is64BitProcess ? "x64" : "x86";
var libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "native", architecture);
Directory.CreateDirectory(libraryPath);

var libwkhtmltoxFile = Path.Combine(libraryPath, "libwkhtmltox.dll");
if (!File.Exists(libwkhtmltoxFile))
{
    var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native", architecture, "libwkhtmltox.dll");
    if (File.Exists(sourceFile))
    {
        File.Copy(sourceFile, libwkhtmltoxFile, true);
    }
    else
    {
        throw new FileNotFoundException($"Required library not found: {sourceFile}");
    }
}

// Update PATH environment variable
var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
if (!currentPath.Contains(libraryPath))
{
    Environment.SetEnvironmentVariable("PATH", $"{currentPath}{Path.PathSeparator}{libraryPath}");
}

// Configure Qt environment variables to suppress messages
Environment.SetEnvironmentVariable("QT_LOGGING_RULES", "qt.qpa.*=false");
Environment.SetEnvironmentVariable("QT_LOGGING_TO_CONSOLE", "0");
Environment.SetEnvironmentVariable("QT_ENABLE_STDERR_LOGGING", "0");

// Register PDF services
builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));
builder.Services.AddSingleton<PdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

