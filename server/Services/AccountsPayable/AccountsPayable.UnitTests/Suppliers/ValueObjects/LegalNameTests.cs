namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class LegalNameTests
{
    // Construir LegalName com texto válido preserva o valor e remove espaços nas pontas.
    [Theory]
    [InlineData("Acme Brasil LTDA", "Acme Brasil LTDA")]
    [InlineData("  Acme Brasil LTDA  ", "Acme Brasil LTDA")]
    public void Constructor_WithValidName_ShouldTrimAndPreserveValue(string raw, string expected)
    {
        var name = new LegalName(raw);

        Assert.Equal(expected, name.Value);
    }

    // Razão social vazia ou só com espaços lança AP.LGN01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new LegalName(raw));

        Assert.Equal("AP.LGN01", ex.Id);
    }

    // Razão social com menos do que o mínimo permitido lança AP.LGN02.
    [Fact]
    public void Constructor_BelowMinLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new LegalName("A"));

        Assert.Equal("AP.LGN02", ex.Id);
    }

    // Razão social acima do MAX_LENGTH lança AP.LGN03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', LegalName.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new LegalName(tooLong));

        Assert.Equal("AP.LGN03", ex.Id);
    }

    // Razão social com exatamente MAX_LENGTH caracteres é aceita (limite inclusivo).
    [Fact]
    public void Constructor_AtExactMaxLength_ShouldSucceed()
    {
        var exact = new string('A', LegalName.MAX_LENGTH);

        var name = new LegalName(exact);

        Assert.Equal(LegalName.MAX_LENGTH, name.Value.Length);
    }

    // Dois LegalNames com mesmo valor são iguais (igualdade por componente).
    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        var a = new LegalName("Acme Brasil LTDA");
        var b = new LegalName("Acme Brasil LTDA");

        Assert.Equal(a, b);
    }
}
