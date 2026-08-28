namespace BillPayment.UnitTests.Retention;

using BillPayment.Domain.Retention;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A janela de retenção do livro-caixa da captura.
/// </summary>
public class CaptureRetentionPolicyTests
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 9, 30, 0, DateTimeKind.Utc);

    // Nasce DESLIGADA, e isso é decisão: uma política que apaga por omissão apagaria o histórico
    // de quem nunca abriu a tela e nem sabe que ele existe.
    [Fact]
    public void Default_ShouldStartDisabledWithTheDefaultWindow()
    {
        var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);

        Assert.False(policy.IsEnabled);
        Assert.Equal(90, policy.Window.Days);
    }

    [Fact]
    public void Configure_ShouldTurnThePurgeOnWithTheChosenWindow()
    {
        var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);

        policy.Configure(isEnabled: true, RetentionWindow.ThirtyDays, OccurredAt);

        Assert.True(policy.IsEnabled);
        Assert.Equal(30, policy.Window.Days);
    }

    // Desligar mantém o prazo à vista: o número continua na tela, e deixá-lo nulo faria a
    // interface inventar um valor para mostrar.
    [Fact]
    public void Configure_WhenDisabling_ShouldKeepTheWindow()
    {
        var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);
        policy.Configure(isEnabled: true, RetentionWindow.SevenDays, OccurredAt);

        policy.Configure(isEnabled: false, RetentionWindow.SevenDays, OccurredAt);

        Assert.False(policy.IsEnabled);
        Assert.Equal(7, policy.Window.Days);
    }

    // A faixa é fechada porque o prazo chega pela API. Zero apagaria o histórico no instante em
    // que ele nasce; um número enorme o transformaria em arquivo permanente da caixa de alguém.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(45)]
    [InlineData(365)]
    [InlineData(-30)]
    public void WindowFromDays_OutsideTheOfferedRange_Throws_BLP_CRP02(int days)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureRetentionPolicy.WindowFromDays(days));

        Assert.Equal("BLP.CRP02", exception.Id);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(180)]
    public void WindowFromDays_WithinTheOfferedRange_ShouldResolve(int days)
    {
        var window = CaptureRetentionPolicy.WindowFromDays(days);

        Assert.Equal(days, window.Days);
    }

    // O corte conta a partir da data de recebimento, que é a que a pessoa vê na tela e a que ela
    // usa para raciocinar sobre "os últimos 30 dias".
    [Fact]
    public void CutoffAt_ShouldSubtractTheWindowFromNow()
    {
        var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);
        policy.Configure(isEnabled: true, RetentionWindow.ThirtyDays, OccurredAt);

        var cutoff = policy.CutoffAt(new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc), cutoff);
    }

    // O Id do Smart Enum É a quantidade de dias — a coluna gravada se lê sozinha, e a purga não
    // precisa de tabela de tradução.
    [Fact]
    public void RetentionWindow_ShouldUseTheDayCountAsItsId()
    {
        Assert.Equal(RetentionWindow.NinetyDays.Days, RetentionWindow.NinetyDays.Id);
        Assert.Equal(4, Enumeration.GetAll<RetentionWindow>().Count());
    }
}
