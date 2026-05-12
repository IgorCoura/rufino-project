namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class DescriptionTests
{
    // Descrição válida é preservada após trim.
    [Fact]
    public void Constructor_WithValidText_ShouldTrimAndPreserve()
    {
        var description = new Description("  Aluguel sede março  ");

        Assert.Equal("Aluguel sede março", description.Value);
    }

    // Descrição vazia ou só whitespace lança AP.DSC01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Constructor_WithEmptyText_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new Description(raw));

        Assert.Equal("AP.DSC01", ex.Id);
    }

    // Descrição acima de MAX_LENGTH lança AP.DSC02.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', Description.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new Description(tooLong));

        Assert.Equal("AP.DSC02", ex.Id);
    }

    // Descrição com exatamente MAX_LENGTH é aceita (limite inclusivo).
    [Fact]
    public void Constructor_AtExactMaxLength_ShouldSucceed()
    {
        var exact = new string('A', Description.MAX_LENGTH);

        var description = new Description(exact);

        Assert.Equal(Description.MAX_LENGTH, description.Value.Length);
    }
}
