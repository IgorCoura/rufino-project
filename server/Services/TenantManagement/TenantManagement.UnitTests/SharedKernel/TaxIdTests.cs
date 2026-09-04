namespace TenantManagement.UnitTests.SharedKernel;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

public class TaxIdTests
{
    private const string ValidCpf = "52998224725";
    private const string ValidCnpj = "11222333000181";

    // CPF e CNPJ válidos são aceitos e guardados só com dígitos.
    [Theory]
    [InlineData("529.982.247-25", ValidCpf)]
    [InlineData("11.222.333/0001-81", ValidCnpj)]
    public void Parse_WithMaskedValidDocument_ShouldKeepOnlyDigits(string input, string expected)
    {
        Assert.Equal(expected, TaxId.Parse(input).Value);
    }

    // O tipo é deduzido pela quantidade de dígitos — o formulário manda o documento sem rótulo.
    [Theory]
    [InlineData(ValidCpf, "CPF")]
    [InlineData(ValidCnpj, "CNPJ")]
    public void Parse_ShouldInferKindFromLength(string input, string expectedKind)
    {
        Assert.Equal(expectedKind, TaxId.Parse(input).Kind.Name);
    }

    // Documento sem tipo informado no construtor é reprovado em TNM.TAX01.
    [Fact]
    public void Constructor_WithoutKind_ShouldThrow_TNM_TAX01()
    {
        var error = Assert.Throws<DomainException>(() => new TaxId(ValidCpf, null!));

        Assert.Equal("TNM.TAX01", error.Id);
    }

    // Documento com quantidade de dígitos que não é nem CPF nem CNPJ é reprovado em TNM.TAX02.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("529982247251")]
    public void Parse_WithInvalidLength_ShouldThrow_TNM_TAX02(string input)
    {
        var error = Assert.Throws<DomainException>(() => TaxId.Parse(input));

        Assert.Equal("TNM.TAX02", error.Id);
    }

    // Dígito verificador errado é reprovado em TNM.TAX03 — é o que impede cadastrar documento inventado.
    [Theory]
    [InlineData("52998224726")]
    [InlineData("11222333000182")]
    public void Parse_WithWrongCheckDigit_ShouldThrow_TNM_TAX03(string input)
    {
        var error = Assert.Throws<DomainException>(() => TaxId.Parse(input));

        Assert.Equal("TNM.TAX03", error.Id);
    }

    // Documento com todos os dígitos iguais casa no comprimento mas não existe: reprovado em TNM.TAX03.
    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000000")]
    public void Parse_WithRepeatedDigits_ShouldThrow_TNM_TAX03(string input)
    {
        var error = Assert.Throws<DomainException>(() => TaxId.Parse(input));

        Assert.Equal("TNM.TAX03", error.Id);
    }

    // TryParse devolve false em vez de lançar quando o documento não presta.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("52998224726")]
    public void TryParse_WithInvalidDocument_ShouldReturnFalse(string? input)
    {
        Assert.False(TaxId.TryParse(input, out var taxId));
        Assert.Null(taxId);
    }

    // TryParse devolve o VO quando o documento é válido.
    [Fact]
    public void TryParse_WithValidDocument_ShouldReturnTrue()
    {
        Assert.True(TaxId.TryParse(ValidCnpj, out var taxId));
        Assert.Equal(ValidCnpj, taxId!.Value);
    }

    // A formatação para exibição segue a máscara de cada tipo.
    [Theory]
    [InlineData(ValidCpf, "529.982.247-25")]
    [InlineData(ValidCnpj, "11.222.333/0001-81")]
    public void Formatted_ShouldRenderTheBrazilianMask(string input, string expected)
    {
        Assert.Equal(expected, TaxId.Parse(input).Formatted());
    }

    // Mesmo número com máscaras diferentes é o mesmo documento — igualdade por valor.
    [Fact]
    public void Equals_WithSameDigits_ShouldBeTrue()
    {
        Assert.Equal(TaxId.Parse("529.982.247-25"), TaxId.Parse(ValidCpf));
    }
}
