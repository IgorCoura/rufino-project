namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class DateRangeTests
{
    private static readonly DateOnly Jan01 = new(2025, 1, 1);
    private static readonly DateOnly Jan07 = new(2025, 1, 7);
    private static readonly DateOnly Jan10 = new(2025, 1, 10);
    private static readonly DateOnly Jan15 = new(2025, 1, 15);
    private static readonly DateOnly Jan31 = new(2025, 1, 31);

    // Construção válida com From <= To preserva os componentes.
    [Fact]
    public void Constructor_WithValidRange_ShouldStoreValues()
    {
        var range = new DateRange(Jan01, Jan31);

        Assert.Equal(Jan01, range.From);
        Assert.Equal(Jan31, range.To);
    }

    // From == To é range válido de exatamente 1 dia.
    [Fact]
    public void Constructor_WithEqualFromAndTo_ShouldBeValidSingleDayRange()
    {
        var range = new DateRange(Jan15, Jan15);

        Assert.Equal(1, range.Days);
    }

    // From > To lança SHK.DRG01 - InvalidRange.
    [Fact]
    public void Constructor_WithFromAfterTo_ShouldThrowSHK_DRG01()
    {
        var ex = Assert.Throws<DomainException>(() => new DateRange(Jan31, Jan01));

        Assert.Equal("SHK.DRG01", ex.Id);
    }

    // Days conta inclusivamente ambas as bordas (Jan01..Jan07 = 7 dias).
    [Fact]
    public void Days_ShouldCountBothEndsInclusive()
    {
        var range = new DateRange(Jan01, Jan07);

        Assert.Equal(7, range.Days);
    }

    // Contains retorna true para bordas (From e To) e qualquer data interna.
    [Theory]
    [InlineData(1)]   // From
    [InlineData(15)]  // middle
    [InlineData(31)]  // To
    public void Contains_WithDateInsideRange_ShouldReturnTrue(int day)
    {
        var range = new DateRange(Jan01, Jan31);

        Assert.True(range.Contains(new DateOnly(2025, 1, day)));
    }

    // Contains retorna false para data anterior a From ou posterior a To.
    [Theory]
    [InlineData(2024, 12, 31)]
    [InlineData(2025, 2, 1)]
    public void Contains_WithDateOutsideRange_ShouldReturnFalse(int y, int m, int d)
    {
        var range = new DateRange(Jan01, Jan31);

        Assert.False(range.Contains(new DateOnly(y, m, d)));
    }

    // Overlaps retorna true para sobreposição parcial e total (contenção bidirecional).
    [Fact]
    public void Overlaps_WithPartialOverlap_ShouldReturnTrue()
    {
        var a = new DateRange(Jan01, Jan15);
        var b = new DateRange(Jan10, Jan31);

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    // Overlaps retorna true quando um range está totalmente contido no outro.
    [Fact]
    public void Overlaps_WithFullContainment_ShouldReturnTrue()
    {
        var outer = new DateRange(Jan01, Jan31);
        var inner = new DateRange(Jan10, Jan15);

        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    // Overlaps retorna true quando ranges tocam em apenas um dia (borda compartilhada).
    [Fact]
    public void Overlaps_WhenRangesShareOneDay_ShouldReturnTrue()
    {
        var a = new DateRange(Jan01, Jan15);
        var b = new DateRange(Jan15, Jan31);

        Assert.True(a.Overlaps(b));
    }

    // Overlaps retorna false para ranges disjuntos.
    [Fact]
    public void Overlaps_WithDisjointRanges_ShouldReturnFalse()
    {
        var a = new DateRange(Jan01, Jan07);
        var b = new DateRange(Jan10, Jan31);

        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    // Dois DateRange com mesmo From e To são iguais (igualdade estrutural).
    [Fact]
    public void Equals_SameFromAndTo_ShouldBeTrue()
    {
        var a = new DateRange(Jan01, Jan31);
        var b = new DateRange(Jan01, Jan31);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // From ou To diferentes produzem instâncias não iguais.
    [Fact]
    public void Equals_DifferentTo_ShouldBeFalse()
    {
        var a = new DateRange(Jan01, Jan31);
        var b = new DateRange(Jan01, Jan15);

        Assert.NotEqual(a, b);
    }
}
