using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace FirstProject.Services
{
    public class PdfService
    {
        public async Task<byte[]> GeneratePdfAsync(string htmlContent)
        {
            // Download Chromium if not already downloaded
            await new BrowserFetcher().DownloadAsync();

            // Launch browser
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            // Create new page
            await using var page = await browser.NewPageAsync();

            // Set content and wait for page to load
            await page.SetContentAsync(htmlContent);
            await page.WaitForTimeoutAsync(1000); // Wait for content to render

            // Generate PDF with updated API - use PdfDataAsync for byte array
            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "1cm",
                    Right = "1cm", 
                    Bottom = "1cm",
                    Left = "1cm"
                }
            });

            return pdfBytes ?? Array.Empty<byte>();
        }

        // Keep the old synchronous method for backward compatibility but make it call the async version
        public byte[] GeneratePdf(string htmlContent)
        {
            return GeneratePdfAsync(htmlContent).GetAwaiter().GetResult();
        }
    }
}
