namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Services;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.UnitTests.TrustedOrigins.Mothers;

/// <summary>
/// A regra que decide se um artefato vira item ou desaparece.
/// </summary>
/// <remarks>
/// Decisão do usuário em 2026-08-11: descartar é o padrão, porque um balde cheio de e-mail
/// irrelevante é um balde que ninguém olha. A exceção é o remetente cadastrado.
/// </remarks>
public class CaptureTriageServiceTests
{
    private static readonly DigitableLine Line = DigitableLine.Parse(
        "34191234546789012345767890123457314880000061507",
        new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));

    private static ExtractionResult Found()
        => ExtractionResult.Found([PaymentInstrument.FromBarcode(Line)], ExtractionMethod.EmbeddedText);

    // Achou instrumento válido: segue para a consulta oficial, venha de quem vier.
    [Fact]
    public void Decide_WhenInstrumentFound_ShouldParseRegardlessOfSender()
    {
        Assert.Same(CaptureTriageDecision.Parse, CaptureTriageService.Decide(Found(), origin: null));
        Assert.Same(
            CaptureTriageDecision.Parse,
            CaptureTriageService.Decide(Found(), TrustedOriginMother.TrustedAddress()));
    }

    // Nada encontrado e remetente desconhecido: o item NÃO chega a existir. É o caso comum numa
    // caixa de uso misto, e é o que mantém a fila utilizável.
    [Fact]
    public void Decide_WhenNothingFoundAndSenderIsUnknown_ShouldDrop()
    {
        var decision = CaptureTriageService.Decide(ExtractionResult.NotFound("no_instrument"), origin: null);

        Assert.Same(CaptureTriageDecision.Drop, decision);
    }

    // Nada encontrado, MAS o remetente é cadastrado: provável falha do parser, não ausência de
    // boleto. Fica na quarentena para alguém informar a linha à mão.
    [Fact]
    public void Decide_WhenNothingFoundButSenderIsKnown_ShouldQuarantine()
    {
        var decision = CaptureTriageService.Decide(
            ExtractionResult.NotFound("no_instrument"), TrustedOriginMother.TrustedAddress());

        Assert.Same(CaptureTriageDecision.Quarantine, decision);
    }

    // Origem banida não ganha a exceção: o tenant já disse que não quer nada dali, e manter o
    // item contrariaria a decisão dele em vez de protegê-lo.
    [Fact]
    public void Decide_WhenSenderIsBlocked_ShouldDrop()
    {
        var decision = CaptureTriageService.Decide(
            ExtractionResult.NotFound("no_instrument"), TrustedOriginMother.BlockedAddress());

        Assert.Same(CaptureTriageDecision.Drop, decision);
    }

    // PDF cifrado de remetente conhecido aguarda senha — não se sabe o que há dentro, e é
    // justamente o não saber que exige a pessoa.
    [Fact]
    public void Decide_WhenLockedAndSenderIsKnown_ShouldLock()
    {
        var decision = CaptureTriageService.Decide(
            ExtractionResult.Locked(), TrustedOriginMother.TrustedAddress());

        Assert.Same(CaptureTriageDecision.Lock, decision);
    }

    // PDF cifrado de remetente desconhecido é descartado como qualquer outro: manter tudo que
    // não abre encheria a fila com o que ninguém pediu.
    [Fact]
    public void Decide_WhenLockedAndSenderIsUnknown_ShouldDrop()
    {
        var decision = CaptureTriageService.Decide(ExtractionResult.Locked(), origin: null);

        Assert.Same(CaptureTriageDecision.Drop, decision);
    }

    // Extração sem resultado exige motivo — sem ele não há como medir a cascata.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NotFound_WithoutReasonCode_ShouldThrowBLP_EXT02(string reasonCode)
    {
        var exception = Assert.Throws<BillPayment.Domain.SeedWork.DomainException>(
            () => ExtractionResult.NotFound(reasonCode));

        Assert.Equal("BLP.EXT02", exception.Id);
    }

    // A senha nunca aparece em texto — nem por interpolação distraída em log ou exceção.
    [Fact]
    public void PasswordCandidate_ToString_ShouldNeverRevealTheSecret()
    {
        var candidate = PasswordCandidate.From("12345678", "cnpj_first_8");

        Assert.DoesNotContain("12345678", candidate.ToString(), StringComparison.Ordinal);
        Assert.Contains("cnpj_first_8", candidate.ToString(), StringComparison.Ordinal);
    }

    // Senha candidata sem rótulo seria senha sem evidência auditável — BLP.EXT03.
    [Fact]
    public void PasswordCandidate_WithoutLabel_ShouldThrowBLP_EXT03()
    {
        var exception = Assert.Throws<BillPayment.Domain.SeedWork.DomainException>(
            () => PasswordCandidate.From("12345678", "  "));

        Assert.Equal("BLP.EXT03", exception.Id);
    }
}
