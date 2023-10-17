using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Commom.FileManager.Pdf
{
    public class PdfManager
    {
        public static async Task ConvertHtml2Pdf(string htmlFilePath, string outputPdfPath)
        {
            var html = await File.ReadAllTextAsync(htmlFilePath);

            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            await using var page = await browser.NewPageAsync();

            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), htmlFilePath);
            await page.GoToAsync("file:" + htmlPath);

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4, 
                PrintBackground = true, 
            };

            await page.PdfAsync(outputPdfPath, pdfOptions);

            await browser.CloseAsync();

        }
    }
}
