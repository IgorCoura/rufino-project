using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PeopleManagement.Infra.Services
{
    public class PdfService : IPdfService
    {
        private readonly TemplatesPathOptions _templatesPathOptions;

        public PdfService(TemplatesPathOptions templatesPathOptions)
        {
            _templatesPathOptions = templatesPathOptions;
        }

        public async Task<byte[]> ConvertHtml2Pdf(DocumentType type, string values, CancellationToken cancellationToken = default)
        {
            //var cd = Directory.GetCurrentDirectory(); //DEGUB 
            await DownloadBrowser();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            await using var page = await browser.NewPageAsync();

            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), type.GetBodyPath(_templatesPathOptions.Source));
            await page.GoToAsync("file:" + htmlPath);

            var contentPage = await page.GetContentAsync();

            var htmlContent = await HtmlService.CreateTemporaryHtmlTemplate(contentPage, type, values, _templatesPathOptions.Source, cancellationToken);

            await page.SetContentAsync(htmlContent.Body);

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = htmlContent.Header,
                FooterTemplate = htmlContent.Footer,
            };

            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "temp", $"{Guid.NewGuid()}.pdf");
            await page.PdfAsync(path, pdfOptions);

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            await browser.CloseAsync();

            return pdfBytes;
        }

        private static async Task DownloadBrowser()
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        
    }
}
