namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Expectations.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// A verificação 14 (<c>ExpectationMatch</c>): chegou uma conta que alguém esperava?
/// </summary>
/// <remarks>
/// Teto de Atenção em todo desfecho — não haver expectativa não desmente nada. O que se prova
/// aqui é a separação entre "era esperada" e "ninguém esperava", e sobretudo a estabilidade do
/// check na revalidação, que é o caminho rotineiro do BC.
/// </remarks>
public class ExpectationCheckTests
{
    /// <summary>O vencimento que o código de barras sintético carrega: 25/06/2026.</summary>
    private static readonly CompetencePeriod BillCompetence = new(2026, 6);

    // Ciclo aberto na competência do vencimento: a conta era esperada, e o check passa limpo.
    [Fact]
    public void Evaluate_WhenACycleWasWaitingForIt_ShouldPass()
    {
        var (expectation, _) = OpenCycleFor(BillCompetence);

        var result = Evaluate(BillWithPayee(), expectation);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Same(RiskLevel.Safe, result.RiskContribution);
    }

    // TESTE-ÂNCORA. Revalidar é rotina — a leitura por IA chega depois da captura e refaz a
    // apuração —, e o cumprimento já fechou o ciclo na primeira passagem. Sem reconhecer o
    // ciclo cumprido por ESTE boleto, o segundo passe encontraria "nenhuma expectativa" e
    // rebaixaria de Seguro para Atenção sozinho, sobre um boleto que cumpriu a conta esperada.
    [Fact]
    public void Evaluate_OnRevalidation_WhenThisBillAlreadyFulfilledTheCycle_ShouldStillPass()
    {
        var bill = BillWithPayee();
        var (expectation, cycle) = OpenCycleFor(BillCompetence);

        BillExpectationMother.Fulfill(
            expectation, cycle.Id, bill.Id, actualDueDate: new DateOnly(2026, 6, 25));

        var result = Evaluate(bill, expectation);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Same(RiskLevel.Safe, result.RiskContribution);
    }

    // Contraprova do anterior: o ciclo cumprido por OUTRO boleto não vale como expectativa
    // deste. Sem ela, a exceção da revalidação viraria "qualquer ciclo cumprido serve".
    [Fact]
    public void Evaluate_WhenAnotherBillFulfilledTheCycle_ShouldNotPass()
    {
        var (expectation, cycle) = OpenCycleFor(BillCompetence);

        BillExpectationMother.Fulfill(
            expectation, cycle.Id, BillId.New(), actualDueDate: new DateOnly(2026, 6, 25));

        var result = Evaluate(BillWithPayee(), expectation);

        Assert.NotEqual(CheckOutcome.Passed, result.Outcome);
        Assert.Same(RiskLevel.Attention, result.RiskContribution);
    }

    // Beneficiário sem nenhuma conta esperada: chegou uma cobrança que ninguém aguardava. É o
    // caso que o recurso existe para expor, e ele vale Atenção — nunca Perigo.
    [Fact]
    public void Evaluate_WithNoExpectationAtAll_ShouldBeAttention()
    {
        var result = Evaluate(BillWithPayee());

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.EXPECTATION_NOT_REGISTERED, result.ReasonCode);
        Assert.Same(RiskLevel.Attention, result.RiskContribution);
    }

    // Sem ciclo para a competência e com UMA expectativa vigiando, o cumprimento abre o ciclo
    // na chegada — então a conta era esperada, e o check reflete isso em vez de acusar ausência.
    [Fact]
    public void Evaluate_WhenTheSoleWatchingExpectationWillOpenTheCycle_ShouldPass()
    {
        var (expectation, _) = OpenCycleFor(new CompetencePeriod(2026, 9));

        var result = Evaluate(BillWithPayee(), expectation);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Equal(CheckReasons.EXPECTATION_CYCLE_OPENS_ON_ARRIVAL, result.ReasonCode);
    }

    // Duas contas do mesmo beneficiário — o caso das quatro instalações da EDP. O serviço não
    // desempata, e o check diz isso em vez de escolher uma.
    [Fact]
    public void Evaluate_WithTwoCandidateAccounts_ShouldBeAmbiguous()
    {
        var (first, _) = OpenCycleFor(BillCompetence, "0000748299879");
        var (second, _) = OpenCycleFor(BillCompetence, "0000748299880");

        var result = Evaluate(BillWithPayee(), first, second);

        Assert.Equal(CheckReasons.EXPECTATION_AMBIGUOUS, result.ReasonCode);
        Assert.Same(RiskLevel.Attention, result.RiskContribution);
    }

    // Expectativa desativada não vigia nada, e dizer "não havia expectativa" seria enganoso —
    // há uma, e alguém a desligou.
    [Fact]
    public void Evaluate_WhenTheExpectationIsDeactivated_ShouldSayItIsPaused()
    {
        var (expectation, _) = OpenCycleFor(BillCompetence);
        expectation.Deactivate("imóvel desocupado", BillExpectationMother.DefaultOccurredAt);

        var result = Evaluate(BillWithPayee(), expectation);

        Assert.Equal(CheckReasons.EXPECTATION_PAUSED, result.ReasonCode);
        Assert.Same(RiskLevel.Attention, result.RiskContribution);
    }

    // Sem beneficiário resolvido não há contra o quê perguntar — e o motivo precisa dizer isso,
    // senão a tela mandaria cadastrar uma conta esperada para um beneficiário desconhecido.
    [Fact]
    public void Evaluate_WithoutAResolvedPayee_ShouldSayThereIsNothingToAskAgainst()
    {
        var result = Evaluate(ValidationMother.BankSlipWithLookup());

        Assert.Equal(CheckReasons.EXPECTATION_PAYEE_UNRESOLVED, result.ReasonCode);
        Assert.Same(RiskLevel.Attention, result.RiskContribution);
    }

    // O teto do Notice, afirmado sobre o catálogo inteiro: nenhum desfecho desta verificação
    // pode passar de Atenção, qualquer que seja o resultado.
    [Fact]
    public void ExpectationCheck_ShouldNeverWeighMoreThanAttention()
    {
        Assert.Same(CheckSeverity.Notice, CheckType.ExpectationMatch.DefaultSeverity);
        Assert.Same(
            RiskLevel.Attention,
            RiskLevel.Of(CheckOutcome.Failed, CheckSeverity.Notice));
    }

    // TESTE DE REGRESSÃO (2026-09-08). IsBlockingFailure era escrito pela NEGATIVA — "severidade
    // diferente de Advisory" —, então o degrau Notice nasceu contando como falha bloqueante: um
    // boleto vencido, cujo teto é Atenção, passava a impedir a aprovação e a aparecer no
    // contador de bloqueios. Nenhuma severidade com teto de Atenção pode bloquear nada.
    [Theory]
    [InlineData(nameof(CheckOutcome.Failed))]
    [InlineData(nameof(CheckOutcome.Warning))]
    [InlineData(nameof(CheckOutcome.Inconclusive))]
    public void NoticeOutcome_ShouldNeverCountAsABlockingFailure(string outcomeName)
    {
        var outcome = Enumeration.FromDisplayName<CheckOutcome>(outcomeName);

        var result = outcome == CheckOutcome.Failed
            ? CheckResult.Failed(
                CheckType.DueDateSanity, CheckReasons.OVERDUE, severity: CheckSeverity.Notice)
            : outcome == CheckOutcome.Warning
                ? CheckResult.Warning(
                    CheckType.DueDateSanity, CheckReasons.OVERDUE, severity: CheckSeverity.Notice)
                : CheckResult.Inconclusive(
                    CheckType.DueDateSanity, CheckReasons.OVERDUE, severity: CheckSeverity.Notice);

        Assert.False(result.IsBlockingFailure);
        Assert.False(result.IsCriticalFailure);
    }

    private static CheckResult Evaluate(Bill bill, params BillExpectation[] expectations)
        => BillValidationService
            .Evaluate(ValidationMother.Context(
                bill,
                payee: ValidationMother.RegisteredPayee(),
                expectations: expectations))
            .Single(r => r.Type == CheckType.ExpectationMatch);

    /// <summary>Boleto consultado e com o beneficiário já resolvido — é o que a 14 exige.</summary>
    private static Bill BillWithPayee()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.ResolvePayee(
            PayeeId.From(BillExpectationMother.DefaultPayee.Value), ValidationMother.OccurredAt);

        return bill;
    }

    private static (BillExpectation Expectation, ExpectationCycle Cycle) OpenCycleFor(
        CompetencePeriod competence, string? accountReference = null)
    {
        var expectation = BillExpectationMother.Register(accountReference);
        var cycle = expectation.OpenCycle(competence, BillExpectationMother.DefaultOccurredAt);

        return (expectation, cycle);
    }
}
