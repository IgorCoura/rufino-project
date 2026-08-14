namespace TenantManagement.UnitTests.SharedKernel;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

public class AddressTests
{
    private static Address Valid() => Address.Create(
        "01310-100", "Avenida Paulista", "1000", "Conj. 51", "Bela Vista", "Sao Paulo", "SP", null);

    // O CEP é guardado só com dígitos e o resto do endereço em caixa alta — uma forma canônica só.
    [Fact]
    public void Create_WithMaskedZipCodeAndMixedCase_ShouldNormalize()
    {
        var address = Valid();

        Assert.Equal("01310100", address.ZipCode);
        Assert.Equal("AVENIDA PAULISTA", address.Street);
        Assert.Equal("BELA VISTA", address.Neighborhood);
        Assert.Equal("SAO PAULO", address.City);
        Assert.Equal("SP", address.State);
    }

    // País não informado vira BRASIL — o cadastro é nacional e exigir o campo só produziria ruído.
    [Fact]
    public void Create_WithoutCountry_ShouldDefaultToBrasil()
    {
        Assert.Equal("BRASIL", Valid().Country);
    }

    // País informado vence o default.
    [Fact]
    public void Create_WithCountry_ShouldKeepIt()
    {
        var address = Address.Create("01310100", "Rua A", "1", null, "Centro", "Cidade", "SP", "Portugal");

        Assert.Equal("PORTUGAL", address.Country);
    }

    // Complemento é o único campo opcional: ausente vira string vazia, não erro.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutComplement_ShouldAcceptEmpty(string? complement)
    {
        var address = Address.Create("01310100", "Rua A", "1", complement, "Centro", "Cidade", "SP", null);

        Assert.Equal(string.Empty, address.Complement);
    }

    // CEP que não tem 8 dígitos é reprovado em TNM.ADR01.
    [Theory]
    [InlineData("")]
    [InlineData("0131010")]
    [InlineData("013101000")]
    [InlineData("CEP")]
    public void Create_WithInvalidZipCode_ShouldThrow_TNM_ADR01(string zipCode)
    {
        var error = Assert.Throws<DomainException>(() =>
            Address.Create(zipCode, "Rua A", "1", null, "Centro", "Cidade", "SP", null));

        Assert.Equal("TNM.ADR01", error.Id);
    }

    // Campo obrigatório em branco é reprovado em TNM.ADR02.
    [Theory]
    [InlineData("", "1", "Centro", "Cidade")]
    [InlineData("Rua A", "", "Centro", "Cidade")]
    [InlineData("Rua A", "1", "", "Cidade")]
    [InlineData("Rua A", "1", "Centro", "")]
    public void Create_WithMissingRequiredField_ShouldThrow_TNM_ADR02(
        string street, string number, string neighborhood, string city)
    {
        var error = Assert.Throws<DomainException>(() =>
            Address.Create("01310100", street, number, null, neighborhood, city, "SP", null));

        Assert.Equal("TNM.ADR02", error.Id);
    }

    // Campo que estoura o limite é reprovado em TNM.ADR03.
    [Fact]
    public void Create_WithOversizedStreet_ShouldThrow_TNM_ADR03()
    {
        var error = Assert.Throws<DomainException>(() => Address.Create(
            "01310100", new string('A', Address.MAX_LENGTH_STREET + 1), "1", null, "Centro", "Cidade", "SP", null));

        Assert.Equal("TNM.ADR03", error.Id);
    }

    // UF só existe como sigla de duas letras: "São Paulo" e "S1" são reprovados em TNM.ADR04.
    [Theory]
    [InlineData("")]
    [InlineData("S")]
    [InlineData("São Paulo")]
    [InlineData("S1")]
    public void Create_WithInvalidState_ShouldThrow_TNM_ADR04(string state)
    {
        var error = Assert.Throws<DomainException>(() =>
            Address.Create("01310100", "Rua A", "1", null, "Centro", "Cidade", state, null));

        Assert.Equal("TNM.ADR04", error.Id);
    }

    // UF minúscula é normalizada para maiúscula em vez de reprovada.
    [Fact]
    public void Create_WithLowercaseState_ShouldUppercaseIt()
    {
        Assert.Equal("MG", Address.Create("30130010", "Rua A", "1", null, "Centro", "Cidade", "mg", null).State);
    }

    // Dois endereços com os mesmos dados são o mesmo endereço — igualdade por valor.
    [Fact]
    public void Equals_WithSameComponents_ShouldBeTrue()
    {
        Assert.Equal(Valid(), Valid());
        Assert.Equal(Valid().GetHashCode(), Valid().GetHashCode());
    }

    // Trocar qualquer componente muda a identidade do VO.
    [Fact]
    public void Equals_WithDifferentNumber_ShouldBeFalse()
    {
        var other = Address.Create("01310-100", "Avenida Paulista", "2000", "Conj. 51", "Bela Vista", "Sao Paulo", "SP", null);

        Assert.NotEqual(Valid(), other);
    }

    // O CEP volta formatado para exibição sem deixar de ser guardado só com dígitos.
    [Fact]
    public void FormattedZipCode_ShouldRenderWithDash()
    {
        Assert.Equal("01310-100", Valid().FormattedZipCode());
    }
}
