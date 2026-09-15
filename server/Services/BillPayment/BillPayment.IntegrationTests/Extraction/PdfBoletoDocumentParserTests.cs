namespace BillPayment.IntegrationTests.Extraction;

using System.Text;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
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

    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime RegisteredAt = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

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

    // REGRESSÃO (2026-09-10, achado do usuário): PDF cujo a senha são os QUATRO primeiros dígitos
    // do CNPJ. Antes de o prefixo 4 entrar na derivação este documento ficava trancado, e o
    // relatório dizia "pdf_locked" — que se lê como "a senha não bate" quando a verdade era que
    // a senha certa nunca tinha sido tentada.
    [Fact]
    public async Task Unlock_WithAFourDigitPrefixPassword_ShouldOpenTheDocument()
    {
        var candidatas = PasswordDerivationService.Derive(
            PayerProfile.Register(
                Tenant, PayerKind.Company, "RUFINO TESTE LTDA", EncryptedPdfFixture.TenantCnpj, RegisteredAt));

        var clear = await Build().UnlockAsync(
            EncryptedPdfFixture.ShortPrefixBytes(), "application/pdf", candidatas, CancellationToken.None);

        Assert.NotNull(clear);

        using var document = PdfDocument.Open(clear!.Value.ToArray());
        Assert.Equal(1, document.NumberOfPages);
    }

    // O sintoma que o usuário viu: o documento era relatado como TRANCADO. "Trancado" manda o item
    // para a quarentena dizendo que a senha não bate, quando a verdade era que a senha certa nunca
    // fora oferecida. Com o prefixo 4 na lista o documento abre, e o desfecho passa a descrever o
    // que de fato aconteceu — este fixture é uma página em branco, então não há instrumento nele.
    [Fact]
    public async Task Parse_WithAFourDigitPrefixPassword_ShouldNotReportTheDocumentAsLocked()
    {
        var candidatas = new[]
        {
            PasswordCandidate.From(EncryptedPdfFixture.ShortPrefixPassword, "cnpj_first_4_primary"),
        };

        var result = await Build().ParseAsync(
            EncryptedPdfFixture.ShortPrefixBytes(), "application/pdf", candidatas,
            knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.IsLocked);

        // A contraprova: sem a candidata de 4 dígitos o mesmo documento volta a ser "trancado".
        var semACandidata = await Build().ParseAsync(
            EncryptedPdfFixture.ShortPrefixBytes(), "application/pdf",
            [PasswordCandidate.From(EncryptedPdfFixture.Password, "cnpj_first_5_primary")],
            knownTaxIds: [], Today, CancellationToken.None);

        Assert.True(semACandidata.IsLocked);
    }

    /// <summary>BR Code dinâmico com CRC válido, o mesmo formato que concessionária emite.</summary>
    private const string DynamicPix =
        "00020101021226770014BR.GOV.BCB.PIX2555api.itau/pix/qr/v2/fe80130d-c5ef-407b-94a5-f6b2005020095"
        + "204000053039865802BR5906SABESP6009SAO PAULO62070503***6304A76E";

    // TESTE DE REGRESSÃO (2026-09-14, fatura da Vivo). O QR Pix vinha desenhado como TEXTO — uma
    // grade de caracteres 0/1 numa fonte de quadradinhos — e não como imagem. O leitor de QR só
    // olhava imagens, então o documento resolvia só pelo código de barras e o trilho Pix sumia em
    // silêncio. Agora a grade é lida e o documento sai com os dois instrumentos.
    [Fact]
    public async Task Parse_WhenTheQrIsDrawnAsAGridOfGlyphs_ShouldReadThePixToo()
    {
        var pdf = PdfWithGlyphQr(DynamicPix, darkGlyph: '1', BankSlip);

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.Contains(result.Instruments, i => i.Kind == PaymentInstrumentKind.PixQr);
        Assert.Contains(result.Instruments, i => i.Kind == PaymentInstrumentKind.Barcode);
    }

    // A fonte decide qual caractere pinta o módulo: o emissor que pinta o 0 também é lido.
    [Fact]
    public async Task Parse_WhenTheGlyphGridPaintsTheZero_ShouldStillReadThePix()
    {
        var pdf = PdfWithGlyphQr(DynamicPix, darkGlyph: '0');

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.Same(PaymentInstrumentKind.PixQr, Assert.Single(result.Instruments).Kind);
        Assert.Same(ExtractionMethod.QrCode, result.Method);
    }

    // A CONTRAPROVA do funil: QR de glifos que não é Pix (um endereço) não vira instrumento — o CRC
    // do BR Code continua sendo quem decide, como no QR em imagem (ADR-011).
    [Fact]
    public async Task Parse_WhenTheGlyphQrIsNotAPixPayload_ShouldNotProduceAnInstrument()
    {
        var pdf = PdfWithGlyphQr("https://www.vivo.com.br/fatura", darkGlyph: '1');

        var result = await Build().ParseAsync(pdf, "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.Resolved);
    }

    // A CONTRAPROVA da detecção: uma grade quadrada de 0/1 que não é QR não produz nada — sem ela,
    // um leitor que devolvesse lixo passaria nos testes acima.
    [Fact]
    public async Task Parse_WhenASquareGridOfDigitsIsNotAQr_ShouldNotProduceAnInstrument()
    {
        var random = new Random(42);
        var rows = Enumerable.Range(0, 29)
            .Select(_ => new string(Enumerable.Range(0, 29).Select(_ => random.Next(2) == 0 ? '0' : '1').ToArray()))
            .ToArray();

        var result = await Build().ParseAsync(PdfWithGrid(rows), "application/pdf", [], knownTaxIds: [], Today, CancellationToken.None);

        Assert.False(result.Resolved);
    }

    /// <summary>
    /// Um PDF com o QR escrito como a Vivo escreve: uma linha de texto por fileira de módulos, com a
    /// margem branca de 4 módulos, e as linhas extras (se houver) acima da grade.
    /// </summary>
    private static byte[] PdfWithGlyphQr(string content, char darkGlyph, params string[] textLines)
    {
        var qr = ZXing.QrCode.Internal.Encoder.encode(content, ZXing.QrCode.Internal.ErrorCorrectionLevel.M);
        var matrix = qr.Matrix;
        const int quiet = 4;
        var side = matrix.Width + (2 * quiet);
        var lightGlyph = darkGlyph == '1' ? '0' : '1';

        var rows = new string[side];
        for (var y = 0; y < side; y++)
        {
            var row = new char[side];
            for (var x = 0; x < side; x++)
            {
                var mx = x - quiet;
                var my = y - quiet;
                var dark = mx >= 0 && my >= 0 && mx < matrix.Width && my < matrix.Height && matrix[mx, my] == 1;
                row[x] = dark ? darkGlyph : lightGlyph;
            }

            rows[y] = new string(row);
        }

        return PdfWithGrid(rows, textLines);
    }

    private static byte[] PdfWithGrid(string[] rows, params string[] textLines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var text = builder.AddStandard14Font(Standard14Font.Helvetica);
        var grid = builder.AddStandard14Font(Standard14Font.Courier);

        var y = 800.0;
        foreach (var line in textLines)
        {
            page.AddText(line, 10, new UglyToad.PdfPig.Core.PdfPoint(30, y), text);
            y -= 20;
        }

        // Courier é monoespaçada: cada caractere cai numa coluna de largura fixa, e as fileiras
        // descem pelo mesmo passo — a grade que a fonte de quadradinhos produz.
        const double size = 4;
        const double step = size * 0.6;
        y -= 20;
        foreach (var row in rows)
        {
            page.AddText(row, size, new UglyToad.PdfPig.Core.PdfPoint(30, y), grid);
            y -= step;
        }

        return builder.Build();
    }
}
