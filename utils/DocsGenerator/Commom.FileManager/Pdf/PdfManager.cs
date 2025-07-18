﻿using Commom.FileManager.Html;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Commom.FileManager.Pdf
{
    public class PdfManager
    {
        public static async Task ConvertHtml2Pdf(HtmlContent htmlContent, string outputPdfPath, string filePdfName)
        {
            var pathPdf = Path.Combine(outputPdfPath, filePdfName);

            if(Directory.Exists(outputPdfPath) is false)
            {
                Directory.CreateDirectory(outputPdfPath);
            }

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            await using var page = await browser.NewPageAsync();

            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), htmlContent.PathFileHtmlBody);
            await page.GoToAsync("file:" + htmlPath);


            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4, 
                PrintBackground = true, 
                DisplayHeaderFooter = true,
                HeaderTemplate = htmlContent.Header,
                FooterTemplate= htmlContent.Footer,
            };

            await page.PdfAsync(pathPdf, pdfOptions);

            await browser.CloseAsync();

        }

        public static async Task DownloadBrowser()
        {
            Console.Write("Baixando dependencias...");
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            Console.Write("Fim.\n");
        }
    }
}
