namespace AccountsPayable.UnitTests.CostCenters.ValueObjects;

using AccountsPayable.Domain.CostCenters.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class CostCenterNameTests
{
    // CostCenterName válido preserva valor após trim.
    [Fact]
    public void Constructor_WithValidName_ShouldTrimAndPreserve()
    {
        var name = new CostCenterName("  Obra Syrah  ");

        Assert.Equal("Obra Syrah", name.Value);
    }

    // Nome vazio lança AP.CCN01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new CostCenterName(raw));

        Assert.Equal("AP.CCN01", ex.Id);
    }

    // Nome com menos do que MIN_LENGTH lança AP.CCN02.
    [Fact]
    public void Constructor_BelowMinLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new CostCenterName("X"));

        Assert.Equal("AP.CCN02", ex.Id);
    }

    // Nome acima de MAX_LENGTH lança AP.CCN03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', CostCenterName.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new CostCenterName(tooLong));

        Assert.Equal("AP.CCN03", ex.Id);
    }
}
