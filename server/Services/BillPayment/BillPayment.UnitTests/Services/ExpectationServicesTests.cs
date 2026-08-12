namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Expectations.Mothers;

/// <summary>
/// O aprendizado que propõe expectativas a partir do histórico — e que recusa quando não pode.
/// </summary>
/// <remarks>
/// <strong>A recusa é tão importante quanto a proposta.</strong> Medido no arquivo real em
/// 2026-08-12: 10 dos 20 grupos de beneficiário têm mais de uma conta do mesmo tenant — quatro
/// instalações da EDP, três do DAE. Uma expectativa por beneficiário seria cumprida pela primeira
/// conta que chegasse e esconderia as outras, que é exatamente a falha silenciosa que o mecanismo
/// existe para impedir.
/// </remarks>
public class ExpectationLearningServiceTests
{
    private static readonly PayeeId Payee = BillExpectationMother.DefaultPayee;

    // Três vencimentos mensais regulares bastam para deduzir recorrência, dia e prazo.
    [Fact]
    public void Propose_WithThreeRegularMonthlyOccurrences_ShouldProposeMonthly()
    {
        var proposal = ExpectationLearningService.Propose(Payee, [
            Occurrence("2026-05-02", "2026-05-10"),
            Occurrence("2026-06-01", "2026-06-10"),
            Occurrence("2026-07-03", "2026-07-10"),
        ]);

        Assert.True(proposal.IsProposal);
        Assert.Same(Recurrence.Monthly, proposal.Recurrence);
        Assert.Equal(10, proposal.ExpectedDueDay);
        Assert.Equal(8, proposal.ObservedLeadDays);
        Assert.Equal(3, proposal.ObservationCount);
    }

    // Duas ocorrências descrevem um intervalo só — não dá para afirmar recorrência nenhuma.
    [Fact]
    public void Propose_WithTwoOccurrences_ShouldRefuseAsTooFew()
    {
        var proposal = ExpectationLearningService.Propose(Payee, [
            Occurrence("2026-06-01", "2026-06-10"),
            Occurrence("2026-07-01", "2026-07-10"),
        ]);

        Assert.False(proposal.IsProposal);
        Assert.Same(LearningRefusal.TooFewOccurrences, proposal.Refusal);
    }

    // Compra avulsa não vira expectativa: sem cadência, alertar seria inventar uma obrigação.
    [Fact]
    public void Propose_WithIrregularSpacing_ShouldRefuseAsIrregular()
    {
        var proposal = ExpectationLearningService.Propose(Payee, [
            Occurrence("2026-01-05", "2026-01-10"),
            Occurrence("2026-02-20", "2026-02-25"),
            Occurrence("2026-07-01", "2026-07-08"),
        ]);

        Assert.False(proposal.IsProposal);
        Assert.Same(LearningRefusal.Irregular, proposal.Refusal);
    }

    // TESTE ÂNCORA DA DECISÃO DA 2.7. Duas contas do mesmo beneficiário vencem no MESMO mês, e é
    // esse o sinal determinístico. Aprender uma expectativa só faria a primeira conta que
    // chegasse cumprir o ciclo e esconder a outra.
    [Fact]
    public void Propose_WhenTwoAccountsShareTheSameMonth_ShouldRefuseAsMultipleAccounts()
    {
        var proposal = ExpectationLearningService.Propose(Payee, [
            Occurrence("2026-05-02", "2026-05-10"),
            Occurrence("2026-05-02", "2026-05-12"),
            Occurrence("2026-06-01", "2026-06-10"),
            Occurrence("2026-06-01", "2026-06-12"),
            Occurrence("2026-07-01", "2026-07-10"),
            Occurrence("2026-07-01", "2026-07-12"),
        ]);

        Assert.False(proposal.IsProposal);
        Assert.Same(LearningRefusal.MultipleAccounts, proposal.Refusal);
    }

    // Conta bimestral é reconhecida pelo intervalo, dentro da tolerância que cobre mês de 28 a 31
    // dias e vencimento empurrado para dia útil.
    [Fact]
    public void Propose_WithBimonthlySpacing_ShouldProposeBimonthly()
    {
        var proposal = ExpectationLearningService.Propose(Payee, [
            Occurrence("2026-01-01", "2026-01-15"),
            Occurrence("2026-03-01", "2026-03-15"),
            Occurrence("2026-05-01", "2026-05-15"),
        ]);

        Assert.True(proposal.IsProposal);
        Assert.Same(Recurrence.Bimonthly, proposal.Recurrence);
    }

    private static BillOccurrence Occurrence(string arrived, string due)
        => new(DateOnly.Parse(arrived, System.Globalization.CultureInfo.InvariantCulture),
            DateOnly.Parse(due, System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>
/// O casamento entre um boleto que chegou e o ciclo que ele cumpre.
/// </summary>
public class ExpectationMatchingServiceTests
{
    private static readonly DateOnly Today = new(2026, 8, 5);

    // O boleto casa pelo VENCIMENTO, não pela data de chegada: é o compromisso que ele liquida
    // que o identifica.
    [Fact]
    public void Match_WhenTheDueDateFallsInsideTheCycleWindow_ShouldMatch()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();

        var match = ExpectationMatchingService.Match([expectation], new DateOnly(2026, 8, 12), Today);

        Assert.NotNull(match);
        Assert.Equal(expectation.Id, match.ExpectationId);
        Assert.Equal(cycle.Id, match.CycleId);
    }

    // Vencimento fora da tolerância é outro compromisso — casar aqui cumpriria o ciclo errado.
    [Fact]
    public void Match_WhenTheDueDateIsFarFromTheCycle_ShouldNotMatch()
    {
        var (expectation, _) = BillExpectationMother.WithOpenCycle();

        Assert.Null(ExpectationMatchingService.Match([expectation], new DateOnly(2026, 10, 20), Today));
    }

    // TESTE ÂNCORA. Duas expectativas do mesmo beneficiário com ciclo aberto na janela — o caso
    // das quatro instalações da EDP. Escolher uma apagaria o alerta da conta que de fato não
    // chegou, então o serviço não escolhe.
    [Fact]
    public void Match_WhenTwoCyclesAreEquallyPlausible_ShouldRefuseToChoose()
    {
        var (first, _) = BillExpectationMother.WithOpenCycle();
        var (second, _) = BillExpectationMother.WithOpenCycle(
            expectation: BillExpectationMother.Register(accountReference: "18502"));

        Assert.Null(ExpectationMatchingService.Match([first, second], new DateOnly(2026, 8, 10), Today));
    }

    // Sem vencimento legível não há casamento: dar por cumprida a conta errada é pior que
    // alertar por uma que chegou.
    [Fact]
    public void Match_WithoutADueDate_ShouldNotMatch()
    {
        var (expectation, _) = BillExpectationMother.WithOpenCycle();

        Assert.Null(ExpectationMatchingService.Match([expectation], billDueDate: null, Today));
    }

    // Expectativa pausada não casa: ela não está monitorando hoje.
    [Fact]
    public void Match_WhenTheExpectationIsPaused_ShouldNotMatch()
    {
        var (expectation, _) = BillExpectationMother.WithOpenCycle();
        expectation.Pause(new DateOnly(2026, 12, 31), BillExpectationMother.DefaultOccurredAt);

        Assert.Null(ExpectationMatchingService.Match([expectation], new DateOnly(2026, 8, 10), Today));
    }
}

/// <summary>O escalonamento — qual aviso vale hoje.</summary>
public class AlertLevelTests
{
    private static readonly DateOnly AlertAt = new(2026, 8, 1);
    private static readonly DateOnly DueDate = new(2026, 8, 10);

    // Antes da data de alerta não há aviso nenhum — é a defesa contra alertar cedo demais.
    [Fact]
    public void DueOn_BeforeTheAlertDate_ShouldReturnNothing()
    {
        Assert.Null(AlertLevel.DueOn(new DateOnly(2026, 7, 25), AlertAt, DueDate));
    }

    // A escalada segue a proximidade do vencimento, e devolve sempre o nível mais alto alcançado
    // — um job parado dois dias manda o aviso que vale hoje, não a fila dos atrasados.
    [Theory]
    [InlineData("2026-08-01", "HeadsUp")]
    [InlineData("2026-08-05", "HeadsUp")]
    [InlineData("2026-08-06", "HeadsUp")]
    [InlineData("2026-08-07", "Warning")]
    [InlineData("2026-08-10", "Urgent")]
    [InlineData("2026-08-11", "Overdue")]
    [InlineData("2026-09-01", "Overdue")]
    public void DueOn_ShouldEscalateWithTheDueDate(string today, string expected)
    {
        var level = AlertLevel.DueOn(
            DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture), AlertAt, DueDate);

        Assert.Equal(expected, level!.Name);
    }
}
