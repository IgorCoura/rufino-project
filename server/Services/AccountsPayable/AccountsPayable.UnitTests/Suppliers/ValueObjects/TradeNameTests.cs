namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class TradeNameTests
{
    // Nome fantasia válido preserva o valor após trim.
    [Fact]
    public void Constructor_WithValidName_ShouldPreserveValue()
    {
        var name = new TradeName("  Acme  ");

        Assert.Equal("Acme", name.Value);
    }

    // Nome fantasia vazio lança AP.TRN01 (uma vez instanciado, não pode ser vazio).
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new TradeName(raw));

        Assert.Equal("AP.TRN01", ex.Id);
    }

    // Nome fantasia acima do MAX_LENGTH lança AP.TRN02.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', TradeName.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new TradeName(tooLong));

        Assert.Equal("AP.TRN02", ex.Id);
    }
}
