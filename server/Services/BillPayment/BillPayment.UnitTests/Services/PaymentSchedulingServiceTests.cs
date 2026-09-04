namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;

/// <summary>
/// O agendamento sob as duas camadas de regra: as do provedor (piso, dia útil, vencido paga na
/// hora) e a política inicial do ADR-017 (24h de antecedência, janela 9h–17h).
/// </summary>
public class PaymentSchedulingServiceTests
{
    private static readonly PaymentSchedulingPolicy Policy = PaymentSchedulingPolicy.Default();
    private static readonly IWorkingDayCalendar EveryDay = new EveryDayCalendar();

    // Segunda-feira, 10h da manhã, dentro da janela.
    private static readonly DateTime MondayMorning = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Unspecified);

    // Boleto já vencido na submissão: o provedor ignora a data e paga na hora — o desfecho é
    // Immediate, que exige confirmação de gente (ADR-017), nunca uma data calculada.
    [Fact]
    public void Resolve_WhenTheBillIsAlreadyOverdue_ShouldRequireImmediateExecution()
    {
        var resolution = PaymentSchedulingService.Resolve(
            requestedDate: new DateOnly(2026, 9, 10),
            dueDate: new DateOnly(2026, 9, 5),
            minimumScheduleDate: null,
            MondayMorning,
            Policy,
            EveryDay);

        Assert.True(resolution.RequiresImmediateExecution);
        Assert.Null(resolution.EffectiveDate);
    }

    // Data pedida com folga passa intacta — a política só mexe em quem chega perto demais.
    [Fact]
    public void Resolve_WithARequestFarAhead_ShouldKeepTheRequestedDate()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 15), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 15), resolution.EffectiveDate);
        Assert.False(resolution.RequiresImmediateExecution);
    }

    // Submetendo às 10h, o expediente de amanhã começa a MENOS de 24h — a data desliza para
    // depois de amanhã. É o "passa para o dia seguinte" do ADR-017.
    [Fact]
    public void Resolve_WhenTomorrowViolatesTheLead_ShouldSlideToTheDayAfter()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 8), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 9), resolution.EffectiveDate);
    }

    // Submetendo exatamente na abertura da janela (9h), amanhã 9h fica a exatas 24h — permitido.
    [Fact]
    public void Resolve_AtWindowOpen_ShouldAllowTheNextDay()
    {
        var atOpen = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Unspecified);

        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 8), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, atOpen, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 8), resolution.EffectiveDate);
    }

    // O piso do provedor (minimumScheduleDate) vence a data pedida.
    [Fact]
    public void Resolve_WithAProviderMinimum_ShouldUseItAsTheFloor()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 10), dueDate: new DateOnly(2026, 9, 25),
            minimumScheduleDate: new DateOnly(2026, 9, 14), MondayMorning, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 14), resolution.EffectiveDate);
    }

    // Dia não útil empurra para o útil seguinte — o ajuste só anda para frente, então nunca
    // desfaz a antecedência já conferida.
    [Fact]
    public void Resolve_OnANonWorkingDay_ShouldSlideToTheNextWorkingDay()
    {
        var calendar = new EveryDayCalendar(nonWorking: new DateOnly(2026, 9, 15));

        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 15), dueDate: new DateOnly(2026, 9, 25),
            minimumScheduleDate: null, MondayMorning, Policy, calendar);

        Assert.Equal(new DateOnly(2026, 9, 16), resolution.EffectiveDate);
    }

    // Vencendo HOJE o boleto ainda não está vencido: segue agendado (com encargos visíveis se o
    // deslize passar do vencimento), nunca imediato — imediato é só o já-vencido.
    [Fact]
    public void Resolve_WhenDueToday_ShouldStillSchedule()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 8), dueDate: new DateOnly(2026, 9, 7),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.False(resolution.RequiresImmediateExecution);
        Assert.NotNull(resolution.EffectiveDate);
    }

    // Deslize composto: a data pedida é apertada pelas TRÊS camadas em sequência — o piso do
    // provedor vence a antecedência de 24h, e o dia não útil empurra o resultado ainda mais
    // para frente. A data final difere da pedida (é o "deslizou" da tela) e segue agendada.
    [Fact]
    public void Resolve_WhenFloorLeadAndCalendarAllBind_ShouldSlideThroughAllThreeLayers()
    {
        // Pedida 07 < piso 08 < antecedência (às 10h de 07, o expediente que honra as 24h
        // começa em 09) — e 09 não é útil, então o resultado final é 10.
        var calendar = new EveryDayCalendar(nonWorking: new DateOnly(2026, 9, 9));
        var requested = new DateOnly(2026, 9, 7);

        var resolution = PaymentSchedulingService.Resolve(
            requested, dueDate: new DateOnly(2026, 9, 25),
            minimumScheduleDate: new DateOnly(2026, 9, 8), MondayMorning, Policy, calendar);

        Assert.Equal(new DateOnly(2026, 9, 10), resolution.EffectiveDate);
        Assert.NotEqual(requested, resolution.EffectiveDate);
        Assert.False(resolution.RequiresImmediateExecution);
    }

    // A janela de submissão é meio-aberta: 9h entra, 17h já não.
    [Theory]
    [InlineData(8, 59, false)]
    [InlineData(9, 0, true)]
    [InlineData(16, 59, true)]
    [InlineData(17, 0, false)]
    public void IsWithinSubmissionWindow_ShouldBeHalfOpen(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, Policy.IsWithinSubmissionWindow(new TimeOnly(hour, minute)));
    }

    // Política incoerente (janela vazia) é defeito de configuração e é recusada na construção.
    [Fact]
    public void Policy_WithAnEmptyWindow_ShouldThrow_BLP_PMO17()
    {
        var ex = Assert.Throws<DomainException>(() => PaymentSchedulingPolicy.Of(
            TimeSpan.FromHours(24), new TimeOnly(17, 0), new TimeOnly(9, 0)));

        Assert.Equal("BLP.PMO17", ex.Id);
    }

    private sealed class EveryDayCalendar(DateOnly? nonWorking = null) : IWorkingDayCalendar
    {
        public bool IsWorkingDay(DateOnly date) => date != nonWorking;

        public DateOnly NextWorkingDayOnOrAfter(DateOnly date)
            => IsWorkingDay(date) ? date : date.AddDays(1);
    }
}
