namespace BillPayment.IntegrationTests.WorkingDays;

using BillPayment.Domain.Ports;
using BillPayment.Infra;
using BillPayment.Infra.WorkingDays;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O calendário que decide a DATA em que dinheiro sai. Resolvido pelo contêiner de DI, no molde
/// do <c>BacenBankDirectoryTests</c>: o risco não é só a aritmética dos feriados — é o registro
/// sumir e a política do ADR-017 passar a agendar em feriado sem erro nenhum.
/// </summary>
public sealed class BrazilianWorkingDayCalendarTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IWorkingDayCalendar _calendar;

    public BrazilianWorkingDayCalendarTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BillPayment"] = "Host=localhost;Database=none",
            })
            .Build();

        _provider = new ServiceCollection()
            .AddInfraDependencies(configuration)
            .BuildServiceProvider();

        _calendar = _provider.GetRequiredService<IWorkingDayCalendar>();
    }

    public void Dispose() => _provider.Dispose();

    // O DI resolve o calendário calculado — trocar o registro por engano quebraria aqui.
    [Fact]
    public void Calendar_ShouldResolveFromDiAsTheBrazilianCalendar()
    {
        Assert.IsType<BrazilianWorkingDayCalendar>(_calendar);
    }

    // Os feriados nacionais de 2026, fixos e móveis (Páscoa de 2026 cai em 05/04): nenhum deles
    // é dia útil bancário — agendar neles faria o provedor deslizar sem a gente saber.
    [Theory]
    [InlineData(2026, 1, 1)]   // Confraternização Universal
    [InlineData(2026, 2, 16)]  // Segunda de Carnaval
    [InlineData(2026, 2, 17)]  // Terça de Carnaval
    [InlineData(2026, 4, 3)]   // Sexta-feira Santa
    [InlineData(2026, 4, 21)]  // Tiradentes
    [InlineData(2026, 5, 1)]   // Dia do Trabalho
    [InlineData(2026, 6, 4)]   // Corpus Christi
    [InlineData(2026, 9, 7)]   // Independência
    [InlineData(2026, 10, 12)] // Nossa Senhora Aparecida
    [InlineData(2026, 11, 2)]  // Finados
    [InlineData(2026, 11, 15)] // Proclamação da República
    [InlineData(2026, 11, 20)] // Consciência Negra (Lei 14.759/2023)
    [InlineData(2026, 12, 25)] // Natal
    public void IsWorkingDay_WithA2026NationalHoliday_ShouldReturnFalse(int year, int month, int day)
    {
        Assert.False(_calendar.IsWorkingDay(new DateOnly(year, month, day)));
    }

    // A CONTRAPROVA da lei: Consciência Negra só é feriado nacional a partir de 2024 —
    // 20/11/2023 caiu numa segunda-feira comum.
    [Fact]
    public void IsWorkingDay_WithConscienciaNegraBeforeTheLaw_ShouldReturnTrue()
    {
        Assert.True(_calendar.IsWorkingDay(new DateOnly(2023, 11, 20)));
    }

    // Fim de semana nunca é dia útil bancário; quarta-feira comum é.
    [Theory]
    [InlineData(2026, 9, 5, false)] // sábado
    [InlineData(2026, 9, 6, false)] // domingo
    [InlineData(2026, 9, 2, true)]  // quarta-feira comum
    public void IsWorkingDay_ShouldRejectWeekendsAndAcceptOrdinaryWeekdays(
        int year, int month, int day, bool expected)
    {
        Assert.Equal(expected, _calendar.IsWorkingDay(new DateOnly(year, month, day)));
    }

    // O deslize composto: Sexta-feira Santa (03/04/2026) + fim de semana → segunda 06/04. É o
    // caminho que o PaymentSchedulingService percorre quando a data cai em ponte de feriado.
    [Fact]
    public void NextWorkingDayOnOrAfter_AcrossGoodFridayAndTheWeekend_ShouldLandOnMonday()
    {
        Assert.Equal(new DateOnly(2026, 4, 6), _calendar.NextWorkingDayOnOrAfter(new DateOnly(2026, 4, 3)));
    }

    // Dia útil devolve ele mesmo — "on or after" inclui a própria data.
    [Fact]
    public void NextWorkingDayOnOrAfter_WithAWorkingDay_ShouldReturnTheSameDay()
    {
        Assert.Equal(new DateOnly(2026, 9, 2), _calendar.NextWorkingDayOnOrAfter(new DateOnly(2026, 9, 2)));
    }

    // A virada de ano: 01/01/2027 é sexta e feriado, então o próximo útil é segunda 04/01 —
    // prova que o cálculo por ano funciona atravessando a fronteira do cache anual.
    [Fact]
    public void NextWorkingDayOnOrAfter_AcrossNewYear_ShouldSkipTheHolidayAndTheWeekend()
    {
        Assert.Equal(new DateOnly(2027, 1, 4), _calendar.NextWorkingDayOnOrAfter(new DateOnly(2027, 1, 1)));
    }
}
