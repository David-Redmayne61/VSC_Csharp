using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using FirstProject.Extensions;
using System.Runtime.InteropServices;

namespace FirstProject.Controllers
{
    public class PdfExportController : Controller
    {
        // Other action methods

        private readonly IConverter _converter;

        public PdfExportController(IConverter converter)
        {
            _converter = converter;
        }

        public IActionResult ExportToPdf()
        {
            var document = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
                    new ObjectSettings() {
                        PagesCount = true,
                        HtmlContent = "<h1>Hello, world!</h1>",
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            var pdf = _converter.ConvertSilently(document);

            return File(pdf, "application/pdf", "document.pdf");
        }
    }
}

namespace FirstProject.Extensions
{
    public static class PdfConverterExtensions
    {
        private static readonly object _lock = new object();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr handle);

        private const int STD_ERROR_HANDLE = -12;

        public static byte[] ConvertSilently(this IConverter converter, HtmlToPdfDocument document)
        {
            lock (_lock)
            {
                var originalErrorHandle = GetStdHandle(STD_ERROR_HANDLE);
                
                try
                {
                    // Redirect stderr to null
                    SetStdHandle(STD_ERROR_HANDLE, IntPtr.Zero);
                    
                    // Redirect console error
                    var originalError = Console.Error;
                    using var errorWriter = new StringWriter();
                    Console.SetError(errorWriter);

                    try
                    {
                        return converter.Convert(document);
                    }
                    finally
                    {
                        Console.SetError(originalError);
                    }
                }
                finally
                {
                    // Restore original stderr handle
                    SetStdHandle(STD_ERROR_HANDLE, originalErrorHandle);
                }
            }
        }
    }
}