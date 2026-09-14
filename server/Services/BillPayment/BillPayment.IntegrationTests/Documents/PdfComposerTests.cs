namespace BillPayment.IntegrationTests.Documents;

using BillPayment.Domain.Ports;
using BillPayment.Infra.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O montador de PDF da exportação de documentos, sem banco e sem HTTP.
/// </summary>
public sealed class PdfComposerTests
{
    private static readonly IPdfComposer Composer = new PdfComposer(NullLogger<PdfComposer>.Instance);

    // Documentos entram na ordem em que foram acrescentados, com todas as páginas.
    [Fact]
    public void TryAppend_WithTwoPdfs_ShouldKeepEveryPageInOrder()
    {
        using var composition = Composer.Begin();

        Assert.True(composition.TryAppend(PdfWithPages("A", 2), "application/pdf"));
        Assert.True(composition.TryAppend(PdfWithPages("B", 3), "application/pdf"));

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(5, document.NumberOfPages);
        Assert.Contains("A-1", document.GetPage(1).Text, StringComparison.Ordinal);
        Assert.Contains("B-1", document.GetPage(3).Text, StringComparison.Ordinal);
        Assert.Contains("B-3", document.GetPage(5).Text, StringComparison.Ordinal);
    }

    // O teto de páginas corta o documento no começo — é a opção "somente a primeira página".
    [Fact]
    public void TryAppend_WithMaxPages_ShouldKeepOnlyTheFirstPages()
    {
        using var composition = Composer.Begin();

        composition.TryAppend(PdfWithPages("A", 4), "application/pdf", maxPages: 1);

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(1, document.NumberOfPages);
        Assert.Contains("A-1", document.GetPage(1).Text, StringComparison.Ordinal);
    }

    // Imagem vira uma página — inclusive WEBP, que o PDF não sabe embutir e é reencodado.
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void TryAppend_WithAnImage_ShouldProduceOnePage(SKEncodedImageFormat format)
    {
        using var composition = Composer.Begin();

        Assert.True(composition.TryAppend(ImageBytes(format), "application/octet-stream"));

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(1, document.NumberOfPages);
        Assert.Single(document.GetPage(1).GetImages());
    }

    // Documento que não abre devolve false e não deixa página nenhuma para trás — quem chama
    // põe o aviso no lugar.
    [Fact]
    public void TryAppend_WithUnreadableBytes_ShouldReturnFalseAndAddNothing()
    {
        using var composition = Composer.Begin();

        Assert.False(composition.TryAppend("%PDF-1.4 corrompido"u8.ToArray(), "application/pdf"));
        Assert.False(composition.TryAppend("<html></html>"u8.ToArray(), "text/html"));
        Assert.Equal(0, composition.PageCount);
    }

    // A página de aviso escreve acentuação do português e quebra o payload Pix, que não tem
    // espaço nenhum, em várias linhas sem perder caractere.
    [Fact]
    public void AppendNotice_WithAccentsAndAnUnbrokenPixPayload_ShouldWriteEverything()
    {
        var payload = string.Concat(Enumerable.Repeat("00020126580014br.gov.bcb.pix0136", 8));
        using var composition = Composer.Begin();

        composition.AppendNotice(new PdfNotice(
            "Boleto sem documento",
            "Não há documento guardado. Código obtido por inserção manual.",
            [new PdfNoticeField("Pix copia e cola", payload)]));

        using var document = PdfDocument.Open(composition.Build());
        var text = string.Concat(document.GetPages().Select(p => p.Text));

        Assert.Contains("Não há documento guardado", text, StringComparison.Ordinal);
        Assert.Contains(payload, text.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    // Aviso maior que uma página continua na seguinte em vez de escrever fora da folha.
    [Fact]
    public void AppendNotice_WhenTheFieldsDoNotFit_ShouldContinueOnANewPage()
    {
        using var composition = Composer.Begin();
        var fields = Enumerable.Range(1, 40).Select(i => new PdfNoticeField($"Campo {i}", $"Valor {i}")).ToList();

        composition.AppendNotice(new PdfNotice("Aviso", "Muitos campos.", fields));

        Assert.True(composition.PageCount > 1);
        using var document = PdfDocument.Open(composition.Build());
        Assert.Contains("Valor 40", document.GetPage(document.NumberOfPages).Text, StringComparison.Ordinal);
    }

    // Composição sem página não vira PDF vazio em silêncio.
    [Fact]
    public void Build_WithoutPages_ShouldThrow()
    {
        using var composition = Composer.Begin();

        Assert.Throws<InvalidOperationException>(() => composition.Build());
    }

    internal static byte[] PdfWithPages(string label, int pages)
    {
        using var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (var i = 1; i <= pages; i++)
            builder.AddPage(595, 842).AddText($"{label}-{i}", 12, new PdfPoint(40, 780), font);

        return builder.Build();
    }

    internal static byte[] ImageBytes(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(120, 80);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(SKColors.SteelBlue);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }
}
