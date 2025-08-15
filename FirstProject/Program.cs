using Microsoft.EntityFrameworkCore;
using FirstProject.Data;
using FirstProject.Services;
using OfficeOpenXml;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configure EPPlus license
OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddScoped<IAuthService, AuthService>();

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

// Add this block after app.Build() but before other middleware
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var authService = services.GetRequiredService<IAuthService>();  // Changed from AuthService to IAuthService
        DbInitializer.Initialize(context, authService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

