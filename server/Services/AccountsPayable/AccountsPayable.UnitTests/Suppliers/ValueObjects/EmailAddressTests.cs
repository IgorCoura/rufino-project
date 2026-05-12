namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class EmailAddressTests
{
    // Email válido é normalizado para minúsculas e o whitespace removido.
    [Theory]
    [InlineData("foo@bar.com", "foo@bar.com")]
    [InlineData("FOO@BAR.COM", "foo@bar.com")]
    [InlineData("  user@domain.com.br  ", "user@domain.com.br")]
    public void Constructor_WithValidEmail_ShouldLowercaseAndTrim(string raw, string expected)
    {
        var email = new EmailAddress(raw);

        Assert.Equal(expected, email.Value);
    }

    // Email vazio ou só espaços lança AP.EML01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new EmailAddress(raw));

        Assert.Equal("AP.EML01", ex.Id);
    }

    // Email malformado (sem @, ou só com parte local) lança AP.EML02.
    [Theory]
    [InlineData("no-arroba")]
    [InlineData("foo@")]
    [InlineData("@bar.com")]
    public void Constructor_WithMalformedEmail_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new EmailAddress(raw));

        Assert.Equal("AP.EML02", ex.Id);
    }

    // Email acima de MAX_LENGTH (254 chars per RFC 5321) lança AP.EML03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var local = new string('a', EmailAddress.MAX_LENGTH);
        var tooLong = $"{local}@domain.com";

        var ex = Assert.Throws<DomainException>(() => new EmailAddress(tooLong));

        Assert.Equal("AP.EML03", ex.Id);
    }

    // Igualdade entre emails compara o valor normalizado (case-insensitive na entrada).
    [Fact]
    public void Equals_WithDifferentCaseSameAddress_ShouldReturnTrue()
    {
        var a = new EmailAddress("foo@bar.com");
        var b = new EmailAddress("FOO@bar.COM");

        Assert.Equal(a, b);
    }
}
