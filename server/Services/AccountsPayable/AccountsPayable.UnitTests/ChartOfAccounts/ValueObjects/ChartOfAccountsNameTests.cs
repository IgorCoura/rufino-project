namespace AccountsPayable.UnitTests.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class ChartOfAccountsNameTests
{
    // Construir ChartOfAccountsName válido preserva o valor após trim.
    [Fact]
    public void Constructor_WithValidName_ShouldTrimAndPreserve()
    {
        var name = new ChartOfAccountsName("  Plano Padrão  ");

        Assert.Equal("Plano Padrão", name.Value);
    }

    // Nome vazio ou só whitespace lança AP.CON01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new ChartOfAccountsName(raw));

        Assert.Equal("AP.CON01", ex.Id);
    }

    // Nome com menos do que MIN_LENGTH caracteres lança AP.CON02.
    [Fact]
    public void Constructor_BelowMinLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ChartOfAccountsName("A"));

        Assert.Equal("AP.CON02", ex.Id);
    }

    // Nome acima de MAX_LENGTH lança AP.CON03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', ChartOfAccountsName.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new ChartOfAccountsName(tooLong));

        Assert.Equal("AP.CON03", ex.Id);
    }
}
