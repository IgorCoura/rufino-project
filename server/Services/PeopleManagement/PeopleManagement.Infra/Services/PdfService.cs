using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;

namespace PeopleManagement.Infra.Services
{
    public class PdfService(DocumentTemplatesOptions documentTemplatesOptions, ILogger<PdfService> logger, IBrowserProvider browserProvider) : IPdfService
    {
        private readonly DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        private readonly ILogger<PdfService> _logger = logger;
        private readonly IBrowserProvider _browserProvider = browserProvider;

        private static readonly SemaphoreSlim _pageSemaphore = new(
            Math.Max(2, Environment.ProcessorCount),
            Math.Max(2, Environment.ProcessorCount));

        private static readonly MemoryCache _templateCache = new(new MemoryCacheOptions
        {
            SizeLimit = 100
        });

        private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetSize(1);

        public async Task<byte[]> ConvertHtml2Pdf(TemplateFileInfo template, string values, CancellationToken cancellationToken = default)
        {
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

            var browser = await _browserProvider.GetBrowserAsync(cancellationToken);

            var tasks = itemsList.Select(item =>
                ThrottledGeneratePageAsync(browser, item.DocumentUnitId, item.Template, item.Content, cancellationToken));

            var results = await Task.WhenAll(tasks);

            _logger.LogInformation("Bulk PDF generation completed for {Count} document units.", results.Length);

            return results;
        }

        private async Task<(Guid DocumentUnitId, byte[] Pdf)> ThrottledGeneratePageAsync(
            IBrowser browser, Guid documentUnitId, TemplateFileInfo template,
            string content, CancellationToken cancellationToken)
        {
            await _pageSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await GeneratePageAsync(browser, documentUnitId, template, content, cancellationToken);
            }
            finally
            {
                _pageSemaphore.Release();
            }
        }

        private async Task<(Guid DocumentUnitId, byte[] Pdf)> GeneratePageAsync(
            IBrowser browser, Guid documentUnitId, TemplateFileInfo template, string content,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Generating PDF for document unit: {DocumentUnitId}", documentUnitId);

            var currentDirectory = Directory.GetCurrentDirectory();
            var templateDirectory = Path.Combine(currentDirectory, _documentTemplatesOptions.SourceDirectory, template.Directory.ToString());
            var headerPath = Path.Combine(templateDirectory, template.HeaderFileName.ToString());
            var footerPath = Path.Combine(templateDirectory, template.FooterFileName.ToString());
            var indexHtmlPath = Path.Combine(templateDirectory, template.BodyFileName.ToString());

            var jsonValues = JsonValue.Parse(content);

            var bodyHtml = await GetCachedTemplate(indexHtmlPath, cancellationToken);
            var processedBody = HtmlService.InsertValuesInHtmlTemplate(jsonValues, bodyHtml);

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync("file://" + indexHtmlPath);
            await page.SetContentAsync(processedBody);

            var headerHtml = GetHtmlContent(headerPath, jsonValues);
            var footerHtml = GetHtmlContent(footerPath, jsonValues);

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = headerHtml,
                FooterTemplate = footerHtml,
                MarginOptions = ExtractMarginOptions(headerHtml, footerHtml),
            };

            var pdfBytes = await page.PdfDataAsync(pdfOptions);

            _logger.LogDebug("PDF generated for document unit: {DocumentUnitId}", documentUnitId);
            return (documentUnitId, pdfBytes);
        }

        private static async Task<string> GetCachedTemplate(string path, CancellationToken cancellationToken)
        {
            if (_templateCache.TryGetValue(path, out string? cached))
                return cached!;

            var content = await File.ReadAllTextAsync(path, cancellationToken);
            _templateCache.Set(path, content, _cacheEntryOptions);
            return content;
        }

        public void InvalidateTemplateCache(string templateDirectory)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var fullDirectory = Path.Combine(currentDirectory, _documentTemplatesOptions.SourceDirectory, templateDirectory);

            if (!Directory.Exists(fullDirectory))
                return;

            var count = 0;
            foreach (var file in Directory.GetFiles(fullDirectory, "*.html"))
            {
                _templateCache.Remove(file);
                count++;
            }

            _logger.LogInformation("Template cache invalidated for directory: {Directory}. Removed {Count} entries.", templateDirectory, count);
        }

        private static readonly Regex _linkStylesheetRegex = new(
            @"<link\s+[^>]*rel\s*=\s*""stylesheet""[^>]*href\s*=\s*""([^""]+)""[^>]*/?>|<link\s+[^>]*href\s*=\s*""([^""]+)""[^>]*rel\s*=\s*""stylesheet""[^>]*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string InlineExternalStyles(string html, string templateDirectory)
        {
            return _linkStylesheetRegex.Replace(html, match =>
            {
                var href = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                var cssPath = Path.Combine(templateDirectory, href.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(cssPath))
                {
                    var css = File.ReadAllText(cssPath);
                    return $"<style>{css}</style>";
                }
                return match.Value;
            });
        }

        private static readonly Regex _marginMetaRegex = new(
            @"<meta\s+name=""pdf-margin-(top|bottom|left|right)""\s+content=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static MarginOptions ExtractMarginOptions(string headerHtml, string footerHtml)
        {
            var margins = new MarginOptions
            {
                Top = "20mm",
                Bottom = "20mm",
                Left = "20mm",
                Right = "20mm",
            };

            foreach (var html in new[] { headerHtml, footerHtml })
            {
                foreach (Match match in _marginMetaRegex.Matches(html))
                {
                    var side = match.Groups[1].Value.ToLower();
                    var value = match.Groups[2].Value;
                    switch (side)
                    {
                        case "top": margins.Top = value; break;
                        case "bottom": margins.Bottom = value; break;
                        case "left": margins.Left = value; break;
                        case "right": margins.Right = value; break;
                    }
                }
            }

            return margins;
        }

        private static string GetHtmlContent(string path, JsonNode? values)
        {
            if (!_templateCache.TryGetValue(path, out string? htmlContentInit))
            {
                htmlContentInit = File.ReadAllText(path);
                _templateCache.Set(path, htmlContentInit, _cacheEntryOptions);
            }

            var templateDir = Path.GetDirectoryName(path)!;
            var inlined = InlineExternalStyles(htmlContentInit!, templateDir);
            return HtmlService.InsertValuesInHtmlTemplate(values, inlined);
        }
    }
}
