using iText.Commons.Utils;
using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.FileManager.Pdf
{
    public class PdfManagerIText
    {
        public static Task ConvertHtml2Pdf(string htmlFilePath, string outputPdfPath)
        {
            using FileStream htmlFile = File.Open(htmlFilePath, FileMode.Open);
            using FileStream pdfDest = File.Open(outputPdfPath, FileMode.Create);
            ConverterProperties converterProperties = new();
            converterProperties.SetFontProvider(new DefaultFontProvider(true, true, true));
            converterProperties.SetBaseUri(htmlFilePath);
            HtmlConverter.ConvertToPdf(htmlFile, pdfDest, converterProperties);
            return Task.CompletedTask;
        }
    }
}
