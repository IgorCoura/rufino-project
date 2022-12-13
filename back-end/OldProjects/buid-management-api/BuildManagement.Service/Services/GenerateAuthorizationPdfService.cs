using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class GenerateAuthorizationPdfService
    {
        private readonly BaseFont _fontBase = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        public void Generate()
        {
            var pxToMm = 72 / 25.2F;
            var pdf = new Document(PageSize.A4, 15 * pxToMm, 15 * pxToMm, 15 * pxToMm, 20 * pxToMm);
            var namePdf = "FileTeste.pdf";
            var file = new FileStream(namePdf, FileMode.Create);
            var writer = PdfWriter.GetInstance(pdf, file);

            pdf.Open();

            var fontParagraph = new iTextSharp.text.Font(_fontBase, 32, iTextSharp.text.Font.NORMAL, BaseColor.Black);

            var title = new Paragraph("Relatório de Pessoas PESOAS", fontParagraph);
            title.Alignment = Element.ALIGN_CENTER;
            pdf.Add(title);

            var table = new PdfPTable(5);
            table.DefaultCell.BorderWidth = 1;
            table.WidthPercentage = 100;

            CreateCell(table,"CODIGO" ,border: 5, horizontalAlignment: PdfCell.ALIGN_CENTER);
            CreateCell(table,"NOME" ,border: 5, horizontalAlignment: PdfCell.ALIGN_LEFT);
            CreateCell(table,"PROFISSAO" ,border: 5,horizontalAlignment: PdfCell.ALIGN_CENTER);
            CreateCell(table,"SALARIO" ,border: 5, horizontalAlignment: PdfCell.ALIGN_CENTER);
            CreateCell(table,"EMPRESA" ,border: 5, horizontalAlignment: PdfCell.ALIGN_CENTER);

            pdf.Add(table);

            table = new PdfPTable(1);
            table.DefaultCell.BorderWidth = 5;
            table.WidthPercentage = 100;

            CreateCell(table, "CODIGO", border: 5, horizontalAlignment: PdfCell.ALIGN_CENTER);


            pdf.Add(table);

            pdf.Close();
            file.Close();



        }

        private void CreateCell(PdfPTable table, string text, 
            int horizontalAlignment = PdfCell.ALIGN_LEFT,
            int border = 1,
            int borderWidthBottom = 1,
            int fixedHeight = 25
            )
        {
            var fontCell = new iTextSharp.text.Font(_fontBase, 12, iTextSharp.text.Font.NORMAL, BaseColor.Black);
            var cell = new PdfPCell(new Phrase(text, fontCell));
            cell.HorizontalAlignment = horizontalAlignment;
            cell.VerticalAlignment = PdfCell.ALIGN_MIDDLE;
            cell.Border = border;
            cell.BorderWidthBottom = borderWidthBottom;
            cell.FixedHeight = fixedHeight;
            table.AddCell(cell);
        }
    }
}
