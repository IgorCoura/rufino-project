namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.SeedWork;

public class EventTimestampTests
{
    // Construção com DateTime UTC preserva o InstantUtc.
    [Fact]
    public void Constructor_WithUtcDateTime_ShouldStoreInstantUtc()
    {
        var instant = new DateTime(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var ts = new EventTimestamp(instant);

        Assert.Equal(instant, ts.InstantUtc);
    }

    // DateTime não-UTC (Local/Unspecified) lança ECC.EVT10.
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_WithNonUtcDateTime_ShouldThrowECC_EVT10(DateTimeKind kind)
    {
        var nonUtc = new DateTime(2025, 10, 1, 12, 0, 0, kind);

        var ex = Assert.Throws<DomainException>(() => new EventTimestamp(nonUtc));

        Assert.Equal("ECC.EVT10", ex.Id);
    }

    // Dois EventTimestamps com mesmo instante são iguais.
    [Fact]
    public void Equals_SameInstant_ShouldBeTrue()
    {
        var instant = new DateTime(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var a = new EventTimestamp(instant);
        var b = new EventTimestamp(instant);

        Assert.Equal(a, b);
    }
}
