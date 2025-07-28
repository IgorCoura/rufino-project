using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.Options;

namespace PeopleManagement.Infra.Services
{
    public class PdfService(LocalStorageOptions localStorageOptions,DocumentTemplatesOptions documentTemplatesOptions, ILogger<PdfService> logger) : IPdfService
    {
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        private readonly LocalStorageOptions _localStorageOptions = localStorageOptions;
        private readonly ILogger<PdfService> _logger = logger;

        public async Task<byte[]> ConvertHtml2Pdf(TemplateFileInfo template, string values, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Converting HTML to PDF for directory: {TemplateName}", template.Directory);
            string cDirectory = Directory.GetCurrentDirectory();
            _logger.LogInformation("---Current Directory: {currentDirectory}---", cDirectory);

            var jsonValues = JsonValue.Parse(values);

            //Path
            var currentDirectory = localStorageOptions.RootPath;
            var templateDirectory = Path.Combine(currentDirectory, _documentTemplatesOptions.SourceDirectory, template.Directory.ToString());;
            var headerPath = Path.Combine(templateDirectory, template.HeaderFileName.ToString());
            var footerPath = Path.Combine(templateDirectory, template.FooterFileName.ToString());
            var indexHtmlPath = Path.Combine(templateDirectory, template.BodyFileName.ToString());

            //Pdf Generate
            _logger.LogInformation("Downloading browser for PDF generation.");
            await DownloadBrowser();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = ["--no-sandbox",  "--disable-setuid-sandbox"] });

            _logger.LogInformation("Browser downloaded successfully. Opening new page for PDF generation.");

            await using var page = await browser.NewPageAsync();

            _logger.LogInformation("Navigating page to HTML template at path: {IndexHtmlPath}", indexHtmlPath);

            await page.GoToAsync("file:" + indexHtmlPath);

            var contentPage = await page.GetContentAsync();

            _logger.LogInformation("Inserting values into HTML template.");
            var newContentPage = await HtmlService.InsertValuesInHtmlTemplate(jsonValues, contentPage); 

            _logger.LogInformation("Setting content for the page with inserted values.");
            await page.SetContentAsync(newContentPage);

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = await GetHtmlContent(headerPath, jsonValues),
                FooterTemplate = await GetHtmlContent(footerPath, jsonValues),
            };
                      

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            await browser.CloseAsync();
            _logger.LogInformation("PDF generation completed successfully.");
            return pdfBytes;
        }

        private static async Task<string> GetHtmlContent(string path, JsonNode? values)
        {
            var htmlContentInit = await File.ReadAllTextAsync(path);
            var htmlContentFinal = await HtmlService.InsertValuesInHtmlTemplate(values, htmlContentInit);
            return htmlContentFinal;
        }

        private static async Task DownloadBrowser()
        {
            
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        
    }
}
