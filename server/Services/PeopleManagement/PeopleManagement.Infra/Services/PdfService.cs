using Newtonsoft.Json.Linq;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Net.Http;

namespace PeopleManagement.Infra.Services
{
    public class PdfService : IPdfService
    {
        private readonly TemplatesPathOptions _templatesPathOptions;

        public PdfService(TemplatesPathOptions templatesPathOptions)
        {
            _templatesPathOptions = templatesPathOptions;
        }

        public async Task<byte[]> ConvertHtml2Pdf(DocumentType type, string content, CancellationToken cancellationToken = default)
        {
            var workDiretory = await HtmlService.CreateTemporaryHtmlTemplate(type, content, _templatesPathOptions.Source, cancellationToken);

            await DownloadBrowser();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            await using var page = await browser.NewPageAsync();

            var bodyHtmlPath = Path.Combine(Directory.GetCurrentDirectory(), type.GetBodyPath(workDiretory));
            await page.GoToAsync("file:" + bodyHtmlPath);

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = await ReadContentFile(type.GetHeaderPath(workDiretory)),
                FooterTemplate = await ReadContentFile(type.GetFooterPath(workDiretory)),
            };

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            await browser.CloseAsync();

            return pdfBytes;
        }

        private static async Task<string> ReadContentFile(string path)
        {
            var content = await File.ReadAllTextAsync(path);
            return content;
        }
        

        private static async Task DownloadBrowser()
        {
            Console.Write("Baixando dependencias...");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            Console.Write("Fim.\n");
        }

        
    }
}
