namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public class InterestAccrualPeriodTests
{
    // GetAll retorna os três períodos de acúmulo: Daily, Monthly e Yearly.
    [Fact]
    public void GetAll_ShouldReturnThreeMembers()
    {
        var all = Enumeration.GetAll<InterestAccrualPeriod>().ToList();

        Assert.Equal(3, all.Count);
    }

    // Período diário conta dias exatos entre vencimento e pagamento.
    [Theory]
    [InlineData(2026, 1, 10, 2026, 1, 11, 1)]
    [InlineData(2026, 1, 10, 2026, 1, 30, 20)]
    [InlineData(2026, 1, 10, 2026, 3, 10, 59)]
    public void ElapsedUnits_Daily_ShouldCountExactDays(
        int dueY, int dueM, int dueD, int paidY, int paidM, int paidD, int expected)
    {
        var due = new DateOnly(dueY, dueM, dueD);
        var paid = new DateOnly(paidY, paidM, paidD);

        Assert.Equal(expected, InterestAccrualPeriod.Daily.ElapsedUnits(due, paid));
    }

    // Período mensal conta diferença de mês-calendário (paridade com a regra vigente: pagar dentro do mês do vencimento não acumula).
    [Theory]
    [InlineData(2025, 10, 20, 2025, 10, 30, 0)]
    [InlineData(2025, 10, 20, 2025, 11, 1, 1)]
    [InlineData(2025, 10, 20, 2025, 12, 1, 2)]
    [InlineData(2025, 12, 31, 2026, 1, 1, 1)]
    public void ElapsedUnits_Monthly_ShouldCountCalendarMonthDifference(
        int dueY, int dueM, int dueD, int paidY, int paidM, int paidD, int expected)
    {
        var due = new DateOnly(dueY, dueM, dueD);
        var paid = new DateOnly(paidY, paidM, paidD);

        Assert.Equal(expected, InterestAccrualPeriod.Monthly.ElapsedUnits(due, paid));
    }

    // Período anual conta diferença de ano-calendário, simétrico à regra mensal.
    [Theory]
    [InlineData(2025, 10, 20, 2025, 12, 31, 0)]
    [InlineData(2025, 10, 20, 2026, 1, 1, 1)]
    [InlineData(2025, 10, 20, 2027, 2, 1, 2)]
    public void ElapsedUnits_Yearly_ShouldCountCalendarYearDifference(
        int dueY, int dueM, int dueD, int paidY, int paidM, int paidD, int expected)
    {
        var due = new DateOnly(dueY, dueM, dueD);
        var paid = new DateOnly(paidY, paidM, paidD);

        Assert.Equal(expected, InterestAccrualPeriod.Yearly.ElapsedUnits(due, paid));
    }

    // Pagamento no vencimento ou antes dele nunca acumula unidade, em qualquer período (clamp em zero).
    [Theory]
    [InlineData("DAILY")]
    [InlineData("MONTHLY")]
    [InlineData("YEARLY")]
    public void ElapsedUnits_PaidOnOrBeforeDueDate_ShouldReturnZero(string periodName)
    {
        var period = Enumeration.FromDisplayName<InterestAccrualPeriod>(periodName);
        var due = new DateOnly(2026, 2, 10);

        Assert.Equal(0, period.ElapsedUnits(due, due));
        Assert.Equal(0, period.ElapsedUnits(due, new DateOnly(2025, 11, 1)));
    }
}
