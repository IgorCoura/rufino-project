namespace BillPayment.IntegrationTests.Extraction;

using System.Text;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Infra.Extraction;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// A cascata sobre PDF de verdade — construído no próprio teste, sem depender de arquivo externo.
/// </summary>
public sealed class PdfBoletoDocumentParserTests
{
    private static readonly DateOnly Today = new(2026, 7, 31);
    private const string BankSlip = "34191234546789012345767890123457314880000061507";

    private static PdfBoletoDocumentParser Build()
        => new(
            Options.Create(new ExtractionOptions()),
            NullLogger<PdfBoletoDocumentParser>.Instance);

    /// <summary>Monta um PDF de uma página com as linhas informadas.</summary>
    private static byte[] PdfWith(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var y = 800;
        foreach (var line in lines)
        {
            page.AddText(line, 10, new UglyToad.PdfPig.Core.PdfPoint(30, y), font);
            y -= 20;
        }

        return builder.Build();
    }

    // PDF com camada de texto e linha digitável: a cascata resolve no degrau barato.
    [Fact]
    public async Task Parse_WithTextLayerCarryingBankSlip_ShouldResolveByEmbeddedText()
    {
        var pdf = PdfWith("Banco Itau S.A.", BankSlip, "Valor: 615,07");

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.True(result.Resolved);
        Assert.Same(ExtractionMethod.EmbeddedText, result.Method);
        Assert.Single(result.Instruments);
        Assert.Null(result.UnlockedBy);
    }

    // PDF com texto mas sem boleto: NotFound com motivo que distingue do PDF sem texto nenhum —
    // é essa distinção que mede se o leitor de QR e o extrator de visão estão sendo necessários.
    [Fact]
    public async Task Parse_WithTextButNoInstrument_ShouldReportNoInstrumentInText()
    {
        var pdf = PdfWith("Contrato de locacao", "Clausula primeira", "CNPJ 12.345.678/0001-90");

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.Resolved);
        Assert.Equal("no_instrument_in_document", result.ReasonCode);
        Assert.False(result.IsLocked);
    }

    // Bytes que não são PDF são recusados antes de qualquer parsing — validar o conteúdo, não a
    // promessa do provedor.
    [Fact]
    public async Task Parse_WithNonPdfBytes_ShouldReportNotAPdf()
    {
        var naoPdf = Encoding.UTF8.GetBytes("PK isto e um zip");

        var result = await Build().ParseAsync(naoPdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.Resolved);
        Assert.Equal("not_a_pdf", result.ReasonCode);
    }

    // Conteúdo vazio não estoura — é o caso do anexo que falhou no download.
    [Fact]
    public async Task Parse_WithEmptyContent_ShouldReportNotAPdf()
    {
        var result = await Build().ParseAsync(
            ReadOnlyMemory<byte>.Empty, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.Resolved);
        Assert.Equal("not_a_pdf", result.ReasonCode);
    }

    // Documento híbrido produz os dois trilhos, que é o que permite o check de consistência.
    [Fact]
    public async Task Parse_WithHybridDocument_ShouldFindBothRails()
    {
        const string brCode =
            "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia5204000053039865802BR5912SABESP TESTE6009SAO PAULO62070503***6304AF33";

        var pdf = PdfWith(BankSlip, brCode);

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.True(result.Resolved);
        Assert.Equal(2, result.Instruments.Count);
    }

    // A senha vazia é tentada primeiro e NÃO conta como "destravado por": não houve derivação.
    [Fact]
    public async Task Parse_WithUnencryptedPdf_ShouldNotClaimAPasswordWasDerived()
    {
        var pdf = PdfWith(BankSlip);
        var candidatas = new[] { PasswordCandidate.From("12345678", "cnpj_first_8") };

        var result = await Build().ParseAsync(pdf, "application/pdf", candidatas, knownTaxIds: [], Today, CancellationToken.None);

        Assert.True(result.Resolved);
        Assert.Null(result.UnlockedBy);
    }

    // Documento que já abre sem senha não tem cópia a produzir — e devolver uma reescreveria um
    // arquivo bom sem motivo. Nulo é "siga com o original", que é o contrato da porta.
    [Fact]
    public async Task Unlock_WithUnencryptedPdf_ShouldReturnNothingToDo()
    {
        var pdf = PdfWith(BankSlip);
        var candidatas = new[] { PasswordCandidate.From("12345678", "cnpj_first_8") };

        var clear = await Build().UnlockAsync(pdf, "application/pdf", candidatas, CancellationToken.None);

        Assert.Null(clear);
    }

    // O que não é PDF não passa por aqui: a porta serve à cifra do PDF, e tentar abrir outra
    // coisa só gastaria trabalho para chegar ao mesmo "siga com o original".
    [Fact]
    public async Task Unlock_WithSomethingThatIsNotAPdf_ShouldReturnNothingToDo()
    {
        var texto = Encoding.UTF8.GetBytes("<html><body>não é PDF</body></html>");

        var clear = await Build().UnlockAsync(texto, "text/html", [], CancellationToken.None);

        Assert.Null(clear);
    }

    // O caso que o fixture cifrado existe para provar: a candidata derivada do CNPJ abre o
    // documento, e a cópia devolvida abre SEM senha nenhuma.
    [Fact]
    public async Task Unlock_WithEncryptedPdf_ShouldReturnACopyThatOpensWithoutAPassword()
    {
        var candidatas = new[] { PasswordCandidate.From(EncryptedPdfFixture.Password, "cnpj_first_5_primary") };

        var clear = await Build().UnlockAsync(
            EncryptedPdfFixture.Bytes(), "application/pdf", candidatas, CancellationToken.None);

        Assert.NotNull(clear);

        // Abrir sem passar senha é a prova; o original, tentado do mesmo jeito, lança.
        using var document = PdfDocument.Open(clear!.Value.ToArray());
        Assert.Equal(1, document.NumberOfPages);
    }

    // A contraprova do fixture: sem a candidata certa o documento continua trancado, e "não
    // consegui" sai igual a "não precisava" — quem distingue os dois é o UnlockedBy.
    [Fact]
    public async Task Unlock_WhenNoCandidateOpensTheDocument_ShouldReturnNothingToDo()
    {
        var candidatas = new[] { PasswordCandidate.From("99999", "cnpj_first_5_primary") };

        var clear = await Build().UnlockAsync(
            EncryptedPdfFixture.Bytes(), "application/pdf", candidatas, CancellationToken.None);

        Assert.Null(clear);
    }
}
