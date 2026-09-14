namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Services;
using BillPayment.UnitTests.Expectations.Mothers;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.PayerProfiles.Mothers;

/// <summary>
/// O número da conta cadastrado na expectativa, procurado no boleto capturado — o degrau 3 da
/// escada de roteamento (ADR-026).
/// </summary>
/// <remarks>
/// O código de barras de <see cref="InstrumentSamples.UtilityLine"/> tem o campo livre
/// <c>5678901234567890123456789</c>: é dali que saem as contas "presentes" destes testes.
/// </remarks>
public class AccountReferenceMatchingServiceTests
{
    private const string AccountInFreeField = "901234567";

    // A conta dentro do campo livre do código de barras, com número longo, é prova forte: o código
    // de barras tem DV, e nove dígitos específicos ali não aparecem por acaso.
    [Fact]
    public void Match_WhenTheAccountIsInsideTheBarcodeFreeField_ShouldMatchAsStrong()
    {
        var expectation = BillExpectationMother.Register(accountReference: AccountInFreeField);

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.UtilityBarcode()], [], [expectation]);

        Assert.NotNull(match);
        Assert.Equal(expectation.Id, match.ExpectationId);
        Assert.Same(RoutingConfidence.Strong, match.Confidence);
    }

    // A conta cadastrada com letras, pontos e zeros à esquerda casa pelos dígitos significativos —
    // o emissor completa o campo à vontade (a Vivo imprime 00001123004411 para a conta 1123004411).
    [Fact]
    public void Match_WhenTheReferenceHasFormattingAndLeadingZeros_ShouldCompareSignificantDigits()
    {
        var expectation = BillExpectationMother.Register(accountReference: "Conta 000.901.234-567");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.UtilityBarcode()], [], [expectation]);

        Assert.NotNull(match);
    }

    // Número curto dentro do código de barras não passa de prova fraca: 6 ou 7 dígitos têm chance
    // real de coincidir num campo livre de 25 posições.
    [Fact]
    public void Match_WhenAShortAccountIsInsideTheBarcode_ShouldMatchAsWeak()
    {
        var expectation = BillExpectationMother.Register(accountReference: "234567");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.UtilityBarcode()], [], [expectation]);

        Assert.Same(RoutingConfidence.Weak, match!.Confidence);
    }

    // Só no texto — do documento ou do e-mail — a conta vale como prova fraca.
    [Fact]
    public void Match_WhenTheAccountAppearsOnlyInTheText_ShouldMatchAsWeak()
    {
        var expectation = BillExpectationMother.Register(accountReference: "1123004411");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.Barcode()],
            ["Telefonica Brasil S.A.Nº da Conta:00001123004411Av. Engenheiro"],
            [expectation]);

        Assert.Same(RoutingConfidence.Weak, match!.Confidence);
    }

    // No texto a conta tem de ser a sequência INTEIRA: dentro de um número maior (um protocolo, um
    // código de barras impresso) o acerto por acaso é plausível.
    [Fact]
    public void Match_WhenTheAccountIsOnlyPartOfALongerNumberInTheText_ShouldNotMatch()
    {
        var expectation = BillExpectationMother.Register(accountReference: "1123004411");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.Barcode()], ["Protocolo 9911230044119"], [expectation]);

        Assert.Null(match);
    }

    // Decisão do usuário (2026-09-14): com menos de 6 dígitos significativos a conta não roteia —
    // a matrícula L18502 do DAE vira 18502 e fica de fora, mesmo presente no documento.
    [Fact]
    public void Match_WhenTheAccountHasFewerThanSixDigits_ShouldNotMatch()
    {
        var expectation = BillExpectationMother.Register(accountReference: "L18502");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.Barcode()], ["Matrícula: 18502"], [expectation]);

        Assert.Null(match);
    }

    // Duas contas do mesmo tenant no mesmo documento: escolher seria adivinhar, e a dúvida vai para
    // a fila de reivindicação.
    [Fact]
    public void Match_WhenTwoExpectationsMatch_ShouldNotChoose()
    {
        var first = BillExpectationMother.Register(accountReference: AccountInFreeField);
        var second = BillExpectationMother.Register(accountReference: "5678901234");

        var match = AccountReferenceMatchingService.Match(
            [InstrumentSamples.UtilityBarcode()], [], [first, second]);

        Assert.Null(match);
    }

    // Expectativa sem conta cadastrada nunca roteia nada.
    [Fact]
    public void Match_WhenTheExpectationHasNoAccountReference_ShouldNotMatch()
    {
        var expectation = BillExpectationMother.Register(accountReference: null);

        Assert.Null(AccountReferenceMatchingService.Match(
            [InstrumentSamples.UtilityBarcode()], ["qualquer texto 901234567"], [expectation]));
    }

    // Dígitos significativos: só dígitos, sem zeros à esquerda, e nulo abaixo de seis.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("12345", null)]
    [InlineData("000012345", null)]
    [InlineData("L-123456", "123456")]
    [InlineData("0000748299879", "748299879")]
    public void SignificantDigits_ShouldKeepOnlyDigitsWithoutLeadingZerosAndRejectShortOnes(
        string? reference, string? expected)
    {
        Assert.Equal(expected, AccountReferenceMatchingService.SignificantDigits(reference));
    }

    // O degrau 3 promove com a força que o casamento trouxe, e só depois de os negativos terem tido
    // a vez: o número foi informado pelo tenant e nunca desfaz a prova de que o boleto é de outro.
    [Fact]
    public void Route_WhenTheAccountReferenceMatched_ShouldPromoteWithTheMatchConfidence()
    {
        var expectation = BillExpectationMother.Register(accountReference: AccountInFreeField);
        var extraction = ExtractionResult.Found([InstrumentSamples.UtilityBarcode()], ExtractionMethod.EmbeddedText);
        var match = AccountReferenceMatchingService.Match(extraction.Instruments, [], [expectation]);

        var decision = BillRoutingService.Route(
            extraction, PayerProfileMother.Register(), [], officialPayerTaxId: null, match);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
        Assert.Equal(BillRoutingService.REASON_ACCOUNT_REFERENCE, decision.Reason);
    }

    // A CONTRAPROVA de isolamento: outra pessoa sob rótulo de pagador continua descartando, mesmo
    // com a conta do tenant presente no documento.
    [Fact]
    public void Route_WhenAnotherPayerIsLabelledAndTheAccountMatched_ShouldStayForeign()
    {
        var expectation = BillExpectationMother.Register(accountReference: AccountInFreeField);
        var extraction = ExtractionResult.Found(
            [InstrumentSamples.UtilityBarcode()],
            ExtractionMethod.EmbeddedText,
            parties: [PartyCandidate.TryCreate("08244181826", underPayerLabel: true)!]);
        var match = AccountReferenceMatchingService.Match(extraction.Instruments, [], [expectation]);

        var decision = BillRoutingService.Route(
            extraction, PayerProfileMother.Register(), [], officialPayerTaxId: null, match);

        Assert.Same(RoutingOutcome.Foreign, decision.Outcome);
    }
}
