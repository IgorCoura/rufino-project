namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;

/// <summary>
/// O agendamento sob as duas camadas de regra: as do provedor (piso, dia útil, vencido paga na
/// hora) e a nossa — que desde o ADR-021 se resume à janela 9h–18h, e só através do
/// <c>CanScheduleForToday</c>.
/// </summary>
public class PaymentSchedulingServiceTests
{
    private static readonly PaymentSchedulingPolicy Policy = PaymentSchedulingPolicy.Default();
    private static readonly IWorkingDayCalendar EveryDay = new EveryDayCalendar();

    // Segunda-feira, 10h da manhã, dentro da janela.
    private static readonly DateTime MondayMorning = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly Today = new(2026, 9, 7);

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

    // Data pedida com folga passa intacta.
    [Fact]
    public void Resolve_WithARequestFarAhead_ShouldKeepTheRequestedDate()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 15), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 15), resolution.EffectiveDate);
        Assert.False(resolution.RequiresImmediateExecution);
    }

    // O teste-âncora do ADR-021: HOJE é hoje. Sob a antecedência de 24h, pedir hoje às 10h
    // produzia depois de amanhã; se este teste voltar a falhar, a antecedência ressuscitou.
    [Fact]
    public void Resolve_WhenTodayIsRequested_ShouldKeepToday()
    {
        var resolution = PaymentSchedulingService.Resolve(
            Today, dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(Today, resolution.EffectiveDate);
    }

    // O irmão do anterior: amanhã também não desliza mais.
    [Fact]
    public void Resolve_WhenTomorrowIsRequested_ShouldKeepTomorrow()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 8), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(new DateOnly(2026, 9, 8), resolution.EffectiveDate);
    }

    // A ordem pode ter dormido na fila e amanhecido com a data pedida no passado — janela
    // fechada, retenção, worker parado. O piso é hoje: submeter data vencida seria recusa.
    [Fact]
    public void Resolve_WhenTheRequestedDateHasPassed_ShouldFloorAtToday()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 4), dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.Equal(Today, resolution.EffectiveDate);
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

    // Dia não útil empurra para o útil seguinte — o ajuste só anda para frente.
    [Fact]
    public void Resolve_OnANonWorkingDay_ShouldSlideToTheNextWorkingDay()
    {
        var calendar = new EveryDayCalendar(nonWorking: new DateOnly(2026, 9, 15));

        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 15), dueDate: new DateOnly(2026, 9, 25),
            minimumScheduleDate: null, MondayMorning, Policy, calendar);

        Assert.Equal(new DateOnly(2026, 9, 16), resolution.EffectiveDate);
    }

    // Vencendo HOJE o boleto ainda não está vencido: segue agendado, nunca imediato.
    [Fact]
    public void Resolve_WhenDueToday_ShouldStillSchedule()
    {
        var resolution = PaymentSchedulingService.Resolve(
            new DateOnly(2026, 9, 8), dueDate: Today,
            minimumScheduleDate: null, MondayMorning, Policy, EveryDay);

        Assert.False(resolution.RequiresImmediateExecution);
        Assert.NotNull(resolution.EffectiveDate);
    }

    // Deslize composto: piso do provedor e dia não útil apertam em sequência. A data final
    // difere da pedida (é o "deslizou" da tela) e segue agendada.
    [Fact]
    public void Resolve_WhenTheFloorAndTheCalendarBothBind_ShouldSlideThroughBoth()
    {
        var calendar = new EveryDayCalendar(nonWorking: new DateOnly(2026, 9, 9));
        var requested = new DateOnly(2026, 9, 7);

        var resolution = PaymentSchedulingService.Resolve(
            requested, dueDate: new DateOnly(2026, 9, 25),
            minimumScheduleDate: new DateOnly(2026, 9, 9), MondayMorning, Policy, calendar);

        Assert.Equal(new DateOnly(2026, 9, 10), resolution.EffectiveDate);
        Assert.NotEqual(requested, resolution.EffectiveDate);
        Assert.False(resolution.RequiresImmediateExecution);
    }

    // Dia útil dentro da janela: hoje serve como data de pagamento, sem motivo de recusa.
    [Fact]
    public void CanScheduleForToday_WithinTheWindowOnAWorkingDay_ShouldAllow()
    {
        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 20), minimumScheduleDate: null,
            MondayMorning, Policy, EveryDay);

        Assert.True(verdict.Allowed);
        Assert.Null(verdict.ReasonCode);
    }

    // Fora da janela a fila só submeteria amanhã, quando "hoje" já é ontem — a opção morre aqui,
    // e não deslizando um dia em silêncio.
    [Fact]
    public void CanScheduleForToday_OutsideTheWindow_ShouldDeny()
    {
        var evening = new DateTime(2026, 9, 7, 18, 30, 0, DateTimeKind.Unspecified);

        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 20), minimumScheduleDate: null,
            evening, Policy, EveryDay);

        Assert.False(verdict.Allowed);
        Assert.Equal(SameDayScheduling.OUTSIDE_WINDOW, verdict.ReasonCode);
    }

    // A janela vale MESMO para vencido: sem submissão hoje não há pagamento hoje, por mais
    // urgente que seja.
    [Fact]
    public void CanScheduleForToday_WhenOverdueButOutsideTheWindow_ShouldDeny()
    {
        var evening = new DateTime(2026, 9, 7, 18, 30, 0, DateTimeKind.Unspecified);

        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 1), minimumScheduleDate: null,
            evening, Policy, EveryDay);

        Assert.False(verdict.Allowed);
        Assert.Equal(SameDayScheduling.OUTSIDE_WINDOW, verdict.ReasonCode);
    }

    // O teste-âncora da ORDEM das perguntas: vencido dispensa piso e calendário, porque o
    // provedor paga na hora. Checar dia útil antes de vencido negaria justamente o caso que
    // funciona — pagar um vencido num sábado dentro da janela.
    [Fact]
    public void CanScheduleForToday_WhenOverdueOnANonWorkingDay_ShouldAllow()
    {
        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 1),
            minimumScheduleDate: new DateOnly(2026, 9, 30),
            MondayMorning, Policy, new EveryDayCalendar(nonWorking: Today));

        Assert.True(verdict.Allowed);
    }

    // Hoje não é dia útil e o boleto não está vencido: o provedor não liquidaria hoje de qualquer
    // forma, então a opção de pagar hoje não é oferecida.
    [Fact]
    public void CanScheduleForToday_OnANonWorkingDay_ShouldDeny()
    {
        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 20), minimumScheduleDate: null,
            MondayMorning, Policy, new EveryDayCalendar(nonWorking: Today));

        Assert.False(verdict.Allowed);
        Assert.Equal(SameDayScheduling.NOT_A_WORKING_DAY, verdict.ReasonCode);
    }

    // Hoje é anterior à primeira data que o provedor aceita para este boleto: pagar hoje não existe.
    [Fact]
    public void CanScheduleForToday_BelowTheProviderMinimum_ShouldDeny()
    {
        var verdict = PaymentSchedulingService.CanScheduleForToday(
            dueDate: new DateOnly(2026, 9, 20),
            minimumScheduleDate: new DateOnly(2026, 9, 9),
            MondayMorning, Policy, EveryDay);

        Assert.False(verdict.Allowed);
        Assert.Equal(SameDayScheduling.PROVIDER_MINIMUM, verdict.ReasonCode);
    }

    // A janela de submissão é meio-aberta: 9h entra, 18h já não (era 17h até o ADR-021).
    [Theory]
    [InlineData(8, 59, false)]
    [InlineData(9, 0, true)]
    [InlineData(17, 59, true)]
    [InlineData(18, 0, false)]
    public void IsWithinSubmissionWindow_ShouldBeHalfOpen(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, Policy.IsWithinSubmissionWindow(new TimeOnly(hour, minute)));
    }

    // Política incoerente (janela vazia) é defeito de configuração e é recusada na construção.
    [Fact]
    public void Policy_WithAnEmptyWindow_ShouldThrow_BLP_PMO17()
    {
        var ex = Assert.Throws<DomainException>(
            () => PaymentSchedulingPolicy.Of(new TimeOnly(18, 0), new TimeOnly(9, 0)));

        Assert.Equal("BLP.PMO17", ex.Id);
    }

    private sealed class EveryDayCalendar(DateOnly? nonWorking = null) : IWorkingDayCalendar
    {
        public bool IsWorkingDay(DateOnly date) => date != nonWorking;

        public DateOnly NextWorkingDayOnOrAfter(DateOnly date)
            => IsWorkingDay(date) ? date : date.AddDays(1);
    }
}
