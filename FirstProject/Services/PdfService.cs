using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;

namespace FirstProject.Services
{
    public class PdfService
    {
        private readonly IConverter _converter;
        private static readonly object _lock = new object();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr handle);

        private const int STD_ERROR_HANDLE = -12;

        public PdfService(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] GeneratePdf(string htmlContent)
        {
            var document = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 25, Bottom = 25, Left = 25, Right = 25 }
                },
                Objects = {
                    new ObjectSettings {
                        PagesCount = true,
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" },
                        FooterSettings = { 
                            Right = "Page [page] of [topage]",
                            Line = true,
                            Spacing = 2.812
                        }
                    }
                }
            };

            lock (_lock)
            {
                var originalErrorHandle = GetStdHandle(STD_ERROR_HANDLE);
                var nullHandle = new IntPtr(-1);
                
                try
                {
                    SetStdHandle(STD_ERROR_HANDLE, nullHandle);
                    return _converter.Convert(document);
                }
                finally
                {
                    SetStdHandle(STD_ERROR_HANDLE, originalErrorHandle);
                }
            }
        }
    }
}