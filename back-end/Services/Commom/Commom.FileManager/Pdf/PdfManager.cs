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
    public class PdfManager
    {
        public static Task ConvertHtml2Pdf(string htmlString, string outputPathPdf)
        {
            using FileStream pdfDest = File.Open(outputPathPdf, FileMode.Create);
            ConverterProperties converterProperties = new();
            converterProperties.SetFontProvider(new DefaultFontProvider(true, true, true));
            converterProperties.SetBaseUri(outputPathPdf);
            HtmlConverter.ConvertToPdf(htmlString, pdfDest, converterProperties);
            return Task.CompletedTask;
        }
    }
}
