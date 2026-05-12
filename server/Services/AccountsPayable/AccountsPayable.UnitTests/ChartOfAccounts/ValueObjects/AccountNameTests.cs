namespace AccountsPayable.UnitTests.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class AccountNameTests
{
    // AccountName válido preserva valor após trim.
    [Fact]
    public void Constructor_WithValidName_ShouldTrimAndPreserve()
    {
        var name = new AccountName("  Despesas operacionais  ");

        Assert.Equal("Despesas operacionais", name.Value);
    }

    // Nome vazio lança AP.ANM01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new AccountName(raw));

        Assert.Equal("AP.ANM01", ex.Id);
    }

    // Nome curto demais lança AP.ANM02.
    [Fact]
    public void Constructor_BelowMinLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new AccountName("A"));

        Assert.Equal("AP.ANM02", ex.Id);
    }

    // Nome longo demais lança AP.ANM03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', AccountName.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new AccountName(tooLong));

        Assert.Equal("AP.ANM03", ex.Id);
    }
}
