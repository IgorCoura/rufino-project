namespace BillPayment.IntegrationTests.DocumentIntelligence;

using BillPayment.Infra.DocumentIntelligence;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O corte de páginas antes de mandar o documento para o extrator.
/// </summary>
/// <remarks>
/// <strong>Teste de regressão.</strong> Bug de 2026-08-11: <c>DocumentIntelligenceOptions.MaxPages</c>
/// existia e <strong>não era aplicado em lugar nenhum</strong> — PDFs inteiros iam para o modelo,
/// incluindo os de dezenas de páginas. Chamadas batiam no timeout de 60 segundos e a vazão do
/// processamento caiu de ~70 para ~8 artefatos por minuto, além de o custo crescer proporcional
/// ao número de páginas sem aumentar a chance de achar o código de barras.
/// </remarks>
public sealed class PdfPageTrimmerTests
{
    // Documento maior que o teto é cortado nas primeiras páginas.
    [Fact]
    public void TakeFirstPages_WhenDocumentIsLonger_ShouldKeepOnlyTheFirstOnes()
    {
        var original = PdfWith(30);

        var trimmed = PdfPageTrimmer.TakeFirstPages(original, maxPages: 5, NullLogger.Instance);

        using var document = PdfDocument.Open(trimmed.ToArray());
        Assert.Equal(5, document.NumberOfPages);
        Assert.True(trimmed.Length < original.Length);
    }

    // Documento dentro do teto passa intacto — cortar de graça só gastaria CPU.
    [Fact]
    public void TakeFirstPages_WhenDocumentIsShort_ShouldReturnItUntouched()
    {
        var original = PdfWith(2);

        var result = PdfPageTrimmer.TakeFirstPages(original, maxPages: 5, NullLogger.Instance);

        Assert.Equal(original.Length, result.Length);
    }

    // O que não é PDF volta como veio: imagem também é mandada ao extrator, e um corte que não
    // deu certo não pode virar documento perdido.
    [Fact]
    public void TakeFirstPages_WhenContentIsNotAPdf_ShouldReturnItUntouched()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };

        var result = PdfPageTrimmer.TakeFirstPages(bytes, maxPages: 5, NullLogger.Instance);

        Assert.Equal(bytes.Length, result.Length);
    }

    // Teto zero ou negativo desliga o corte, em vez de produzir documento sem página nenhuma.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TakeFirstPages_WithoutALimit_ShouldReturnTheWholeDocument(int maxPages)
    {
        var original = PdfWith(10);

        var result = PdfPageTrimmer.TakeFirstPages(original, maxPages, NullLogger.Instance);

        Assert.Equal(original.Length, result.Length);
    }

    private static byte[] PdfWith(int pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (var i = 1; i <= pages; i++)
        {
            var page = builder.AddPage(595, 842);
            page.AddText($"pagina {i}", 12, new UglyToad.PdfPig.Core.PdfPoint(40, 800), font);
        }

        return builder.Build();
    }
}
