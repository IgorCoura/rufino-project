using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;

namespace PeopleManagement.Infra.Services
{
    public class PdfService(DocumentTemplatesOptions documentTemplatesOptions, ILogger<PdfService> logger, IBrowserProvider browserProvider) : IPdfService
    {
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        private readonly ILogger<PdfService> _logger = logger;
        private readonly IBrowserProvider _browserProvider = browserProvider;

        public async Task<byte[]> ConvertHtml2Pdf(TemplateFileInfo template, string values, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Converting HTML to PDF for directory: {TemplateName}", template.Directory);
            var id = Guid.NewGuid();
            var results = await ConvertHtml2PdfRange([(id, template, values)], cancellationToken);
            return results[0].Pdf;
        }

        public async Task<IReadOnlyList<(Guid DocumentUnitId, byte[] Pdf)>> ConvertHtml2PdfRange(
            IEnumerable<(Guid DocumentUnitId, TemplateFileInfo Template, string Content)> items,
            CancellationToken cancellationToken = default)
        {
            var itemsList = items.ToList();
            if (itemsList.Count == 0)
                return [];

            _logger.LogInformation("Starting bulk PDF generation for {Count} document units.", itemsList.Count);
            _logger.LogInformation("---Current Directory: {currentDirectory}---", Directory.GetCurrentDirectory());

            var browser = await _browserProvider.GetBrowserAsync(cancellationToken);

            var tasks = itemsList.Select(item =>
                GeneratePageAsync(browser, item.DocumentUnitId, item.Template, item.Content, cancellationToken));

            var results = await Task.WhenAll(tasks);

            _logger.LogInformation("Bulk PDF generation completed for {Count} document units.", results.Length);

            return results;
        }

        private async Task<(Guid DocumentUnitId, byte[] Pdf)> GeneratePageAsync(
            IBrowser browser, Guid documentUnitId, TemplateFileInfo template, string content,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Generating PDF for document unit: {DocumentUnitId}", documentUnitId);

            var currentDirectory = Directory.GetCurrentDirectory();
            var templateDirectory = Path.Combine(currentDirectory, _documentTemplatesOptions.SourceDirectory, template.Directory.ToString());
            var headerPath = Path.Combine(templateDirectory, template.HeaderFileName.ToString());
            var footerPath = Path.Combine(templateDirectory, template.FooterFileName.ToString());
            var indexHtmlPath = Path.Combine(templateDirectory, template.BodyFileName.ToString());

            var jsonValues = JsonValue.Parse(content);

            await using var page = await browser.NewPageAsync();

            _logger.LogInformation("Navigating page to HTML template at path: {path}", "file://" + indexHtmlPath);
            await page.GoToAsync("file://" + indexHtmlPath);

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

            _logger.LogInformation("PDF generated successfully for document unit: {DocumentUnitId}", documentUnitId);
            return (documentUnitId, pdfBytes);
        }

        private static async Task<string> GetHtmlContent(string path, JsonNode? values)
        {
            var htmlContentInit = await File.ReadAllTextAsync(path);
            var htmlContentFinal = await HtmlService.InsertValuesInHtmlTemplate(values, htmlContentInit);
            return htmlContentFinal;
        }
    }
}
