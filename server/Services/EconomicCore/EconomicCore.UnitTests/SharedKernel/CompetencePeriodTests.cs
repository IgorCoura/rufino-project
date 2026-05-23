namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class CompetencePeriodTests
{
    // Construção válida com Year e Month dentro dos limites preserva os componentes.
    [Fact]
    public void Constructor_WithValidYearAndMonth_ShouldStoreValues()
    {
        var period = new CompetencePeriod(2025, 10);

        Assert.Equal(2025, period.Year);
        Assert.Equal(10, period.Month);
    }

    // Year fora de [MIN_YEAR..MAX_YEAR] lança SHK.PER01 - InvalidYear.
    [Theory]
    [InlineData(1899)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Constructor_WithYearOutOfRange_ShouldThrowSHK_PER01(int year)
    {
        var ex = Assert.Throws<DomainException>(() => new CompetencePeriod(year, 1));

        Assert.Equal("SHK.PER01", ex.Id);
    }

    // Month fora de [1..12] lança SHK.PER02 - InvalidMonth.
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Constructor_WithMonthOutOfRange_ShouldThrowSHK_PER02(int month)
    {
        var ex = Assert.Throws<DomainException>(() => new CompetencePeriod(2025, month));

        Assert.Equal("SHK.PER02", ex.Id);
    }

    // Next em mês <12 incrementa Month no mesmo Year.
    [Fact]
    public void Next_WhenMonthBelowDecember_ShouldIncrementMonth()
    {
        var period = new CompetencePeriod(2025, 1);

        var next = period.Next();

        Assert.Equal(2025, next.Year);
        Assert.Equal(2, next.Month);
    }

    // Next em dezembro avança para janeiro do próximo ano (wrap).
    [Fact]
    public void Next_WhenDecember_ShouldWrapToJanuaryOfNextYear()
    {
        var period = new CompetencePeriod(2025, 12);

        var next = period.Next();

        Assert.Equal(2026, next.Year);
        Assert.Equal(1, next.Month);
    }

    // Previous em mês >1 decrementa Month no mesmo Year.
    [Fact]
    public void Previous_WhenMonthAboveJanuary_ShouldDecrementMonth()
    {
        var period = new CompetencePeriod(2025, 2);

        var previous = period.Previous();

        Assert.Equal(2025, previous.Year);
        Assert.Equal(1, previous.Month);
    }

    // Previous em janeiro retrocede para dezembro do ano anterior (wrap).
    [Fact]
    public void Previous_WhenJanuary_ShouldWrapToDecemberOfPreviousYear()
    {
        var period = new CompetencePeriod(2025, 1);

        var previous = period.Previous();

        Assert.Equal(2024, previous.Year);
        Assert.Equal(12, previous.Month);
    }

    // FirstDay sempre retorna o dia 1 do mês.
    [Fact]
    public void FirstDay_ShouldReturnFirstDayOfMonth()
    {
        var period = new CompetencePeriod(2025, 3);

        Assert.Equal(new DateOnly(2025, 3, 1), period.FirstDay());
    }

    // LastDay respeita a quantidade real de dias do mês (28/29/30/31, com leap year correto).
    [Theory]
    [InlineData(2025, 1, 31)]
    [InlineData(2025, 2, 28)]
    [InlineData(2024, 2, 29)] // leap year
    [InlineData(2025, 4, 30)]
    [InlineData(2025, 12, 31)]
    public void LastDay_ShouldReturnLastCalendarDayOfMonth(int year, int month, int expectedDay)
    {
        var period = new CompetencePeriod(year, month);

        Assert.Equal(new DateOnly(year, month, expectedDay), period.LastDay());
    }

    // Contains retorna true para qualquer data dentro do mês/ano da competência.
    [Theory]
    [InlineData(2025, 10, 1)]
    [InlineData(2025, 10, 15)]
    [InlineData(2025, 10, 31)]
    public void Contains_WithDateInsideMonth_ShouldReturnTrue(int y, int m, int d)
    {
        var period = new CompetencePeriod(2025, 10);

        Assert.True(period.Contains(new DateOnly(y, m, d)));
    }

    // Contains retorna false para data fora do mês ou do ano.
    [Theory]
    [InlineData(2025, 9, 30)]
    [InlineData(2025, 11, 1)]
    [InlineData(2024, 10, 15)]
    [InlineData(2026, 10, 15)]
    public void Contains_WithDateOutsideMonth_ShouldReturnFalse(int y, int m, int d)
    {
        var period = new CompetencePeriod(2025, 10);

        Assert.False(period.Contains(new DateOnly(y, m, d)));
    }

    // Dois CompetencePeriod com mesmo Year+Month são iguais (igualdade estrutural).
    [Fact]
    public void Equals_SameYearAndMonth_ShouldBeTrue()
    {
        var a = new CompetencePeriod(2025, 10);
        var b = new CompetencePeriod(2025, 10);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Year ou Month diferentes produzem instâncias não iguais.
    [Fact]
    public void Equals_DifferentMonth_ShouldBeFalse()
    {
        var a = new CompetencePeriod(2025, 10);
        var b = new CompetencePeriod(2025, 11);

        Assert.NotEqual(a, b);
    }
}
