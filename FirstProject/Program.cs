// Move all using directives to the top of the file to resolve CS1529  
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using FirstProject.Data;
using FirstProject.Services;
using OfficeOpenXml;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication.Cookies;

//ExcelPackage.License = new NonCommercialLicense();

var builder = WebApplication.CreateBuilder(args);

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

// Register PDF services  
builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));
builder.Services.AddSingleton<PdfService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthorization(); // Add this line to register the default authorization services
builder.Services.AddControllersWithViews();
builder.WebHost.UseIISIntegration();

var app = builder.Build();

// Middleware to set anti-caching headers for authenticated requests  
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var authService = services.GetRequiredService<IAuthService>();
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
