namespace BillPayment.IntegrationTests.Extraction;

using BillPayment.Domain.Instruments;
using BillPayment.Infra.Extraction;

/// <summary>
/// O degrau 2 da cascata: achar instrumento de pagamento em texto solto.
/// </summary>
/// <remarks>
/// Roda sem PDF e sem rede — o que está sob teste é a geração de candidatos e o filtro do
/// domínio, que é onde boleto entra ou some.
/// </remarks>
public sealed class CandidateScannerTests
{
    private static readonly DateTime Today = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    private const string BankSlip = "34191234546789012345767890123457314880000061507";
    private const string Utility = "826600000010224812345672890123456786901234567898";
    private const string BrCode =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia5204000053039865802BR5912SABESP TESTE6009SAO PAULO62070503***6304AF33";

    // Linha digitável solta no texto é encontrada.
    [Fact]
    public void Scan_WithPlainBankSlipLine_ShouldFindOneInstrument()
    {
        var found = CandidateScanner.Scan(BankSlip, Today);

        var instrument = Assert.Single(found);
        Assert.Equal(PaymentInstrumentKind.Barcode, instrument.Kind);
    }

    // A formatação que os emissores usam para deixar a linha legível não atrapalha.
    [Theory]
    [InlineData("34191.23454 67890.123457 67890.123457 3 14880000061507")]
    [InlineData("34191.23454  67890.123457  67890.123457  3  14880000061507")]
    [InlineData("34191-23454-67890-123457-67890-123457-3-14880000061507")]
    public void Scan_WithFormattedLine_ShouldStillFindIt(string formatted)
        => Assert.Single(CandidateScanner.Scan(formatted, Today));

    // A linha vem cercada de texto de boleto de verdade — cabeçalho, valor, vencimento.
    [Fact]
    public void Scan_WithLineSurroundedByDocumentText_ShouldFindIt()
    {
        var text = $"""
            Banco Itaú S.A.
            Vencimento: 31/07/2026
            {BankSlip}
            Valor do documento: 615,07
            Beneficiário: FORNECEDOR LTDA
            """;

        Assert.Single(CandidateScanner.Scan(text, Today));
    }

    // Arrecadação tem 48 dígitos e também é encontrada.
    [Fact]
    public void Scan_WithUtilityLine_ShouldFindIt()
    {
        var instrument = Assert.Single(CandidateScanner.Scan(Utility, Today));

        Assert.Equal(PaymentInstrumentKind.Barcode, instrument.Kind);
    }

    // BR Code no texto é encontrado, e o CRC-16 é conferido ao construir.
    [Fact]
    public void Scan_WithBrCode_ShouldFindPixInstrument()
    {
        var instrument = Assert.Single(CandidateScanner.Scan(BrCode, Today));

        Assert.Equal(PaymentInstrumentKind.PixQr, instrument.Kind);
    }

    // Documento híbrido: código de barras e QR na mesma página produzem os dois instrumentos,
    // que é o que permite o check PixBarcodeConsistency comparar as duas histórias.
    [Fact]
    public void Scan_WithHybridDocument_ShouldFindBothRails()
    {
        var found = CandidateScanner.Scan($"{BankSlip}\n{BrCode}", Today);

        Assert.Equal(2, found.Count);
        Assert.Contains(found, i => i.Kind == PaymentInstrumentKind.Barcode);
        Assert.Contains(found, i => i.Kind == PaymentInstrumentKind.PixQr);
    }

    // Boleto com canhoto imprime a linha duas vezes — vira um instrumento só.
    [Fact]
    public void Scan_WithLinePrintedTwice_ShouldDeduplicate()
        => Assert.Single(CandidateScanner.Scan($"{BankSlip}\n\n{BankSlip}", Today));

    // Números que não são boleto não viram candidato: é o que permite descartar sem encher fila.
    [Theory]
    [InlineData("11111111111111111111111111111111111111111111111")]
    [InlineData("Nota fiscal 000123456 emitida em 31/07/2026 no valor de R$ 1.234,56")]
    [InlineData("CNPJ 12.345.678/0001-90 Inscricao Estadual 111.222.333.444")]
    public void Scan_WithNonBoletoText_ShouldFindNothing(string text)
        => Assert.Empty(CandidateScanner.Scan(text, Today));

    // Quebra de linha ENCERRA a sequência: emendar dígitos de linhas diferentes produziria
    // números que não existem no documento, e um deles poderia passar nos DVs por acaso — que é
    // o falso positivo já observado no corpus real.
    [Fact]
    public void Scan_ShouldNotJoinDigitsAcrossLineBreaks()
    {
        var partido = string.Concat(BankSlip.AsSpan(0, 20), "\n", BankSlip.AsSpan(20));

        Assert.Empty(CandidateScanner.Scan(partido, Today));
    }

    // Texto vazio ou ausente não é erro — é o caso do PDF sem camada de texto (18% do corpus).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Scan_WithoutText_ShouldReturnEmpty(string? text)
        => Assert.Empty(CandidateScanner.Scan(text, Today));

    // A varredura tem teto: um documento com muitos dígitos não pode virar um laço caro.
    [Fact]
    public void Scan_WithHugeDigitRun_ShouldTerminateQuickly()
    {
        var enorme = new string('7', 200_000);

        var started = System.Diagnostics.Stopwatch.StartNew();
        var found = CandidateScanner.Scan(enorme, Today);
        started.Stop();

        Assert.Empty(found);
        Assert.True(started.ElapsedMilliseconds < 5_000, $"levou {started.ElapsedMilliseconds}ms");
    }
}
