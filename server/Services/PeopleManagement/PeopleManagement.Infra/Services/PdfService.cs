using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Metadata;

namespace PeopleManagement.Infra.Services
{
    public class PdfService(DocumentTemplatesOptions documentTemplatesOptions) : IPdfService
    {
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;

        public async Task<byte[]> ConvertHtml2Pdf(TemplateFileInfo template, string values, CancellationToken cancellationToken = default)
        {
            var jsonValues = JsonValue.Parse(values);

            //Path
            var currentDirectory = Directory.GetCurrentDirectory();
            var templateDirectory = Path.Combine(currentDirectory, _documentTemplatesOptions.SourceDirectory, template.Directory.ToString());;
            var headerPath = Path.Combine(templateDirectory, template.HeaderFileName.ToString());
            var footerPath = Path.Combine(templateDirectory, template.FooterFileName.ToString());
            var indexHtmlPath = Path.Combine(templateDirectory, template.BodyFileName.ToString());

            //Pdf Generate
            await DownloadBrowser();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = ["--no-sandbox",  "--disable-setuid-sandbox"] });

            await using var page = await browser.NewPageAsync();

            await page.GoToAsync("file:" + indexHtmlPath);

            var contentPage = await page.GetContentAsync();

            var newContentPage = await HtmlService.InsertValuesInHtmlTemplate(jsonValues, contentPage); 

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
