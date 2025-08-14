Redmayne\VSC\VSC_Csharp\FirstProject\README.md
# First Project

A C# ASP.NET Core web application that demonstrates database operations, PDF generation, and Excel export functionality.

## Features

- **Database Management**: CRUD operations for person records
- **PDF Export**: Generate PDF documents with proper formatting and page numbering
- **Excel Export**: Export data to Excel spreadsheets
- **Search Functionality**: Filter records by multiple criteria
- **Sorting**: Sort records by any column

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB or higher)
- Visual Studio Code or Visual Studio 2022

## Getting Started

1. Clone the repository
2. Navigate to the project directory:
```powershell
cd FirstProject
```
3. Restore dependencies:
```powershell
dotnet restore
```
4. Update the database:
```powershell
dotnet ef database update
```
5. Run the application:
```powershell
dotnet run
```

## Configuration

### Database
Connection string can be modified in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FirstProject;Trusted_Connection=True"
  }
}
```

### PDF Generation
The application uses DinkToPdf for PDF generation. Required DLLs are automatically copied to:
- `native/x64/libwkhtmltox.dll` (64-bit)
- `native/x86/libwkhtmltox.dll` (32-bit)

Note: A Qt OLE initialization warning may appear in the development console. This is a known issue and does not affect functionality.

### Excel Export
Uses EPPlus library with NonCommercial license. Configure in `Program.cs`:
```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

## Project Structure

- `/Controllers`: MVC controllers
- `/Models`: Data models and view models
- `/Views`: Razor views
- `/Data`: Database context and migrations
- `/Services`: Business logic services
- `/Extensions`: Extension methods

## Known Issues

1. Qt OLE Initialization Warning
   - Message: "Qt: Could not initialize OLE (error 80010106)"
   - Impact: None (development console only)
   - Status: Working as expected

## License

This project is licensed under the MIT License - see the LICENSE file for details.