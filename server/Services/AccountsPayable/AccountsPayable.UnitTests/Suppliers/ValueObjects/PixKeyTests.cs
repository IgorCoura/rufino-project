namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class PixKeyTests
{
    // PIX tipo CPF: aceita CPF formatado, normaliza para 11 dígitos.
    [Fact]
    public void Constructor_CpfType_WithFormattedInput_ShouldNormalizeToDigits()
    {
        var key = new PixKey("123.456.789-09", PixKeyType.Cpf);

        Assert.Equal("12345678909", key.Value);
        Assert.Equal(PixKeyType.Cpf, key.Type);
    }

    // PIX tipo CNPJ: aceita CNPJ formatado e normaliza para 14 dígitos.
    [Fact]
    public void Constructor_CnpjType_WithFormattedInput_ShouldNormalizeToDigits()
    {
        var key = new PixKey("11.444.777/0001-61", PixKeyType.Cnpj);

        Assert.Equal("11444777000161", key.Value);
        Assert.Equal(PixKeyType.Cnpj, key.Type);
    }

    // PIX tipo Email: normaliza para minúsculas.
    [Fact]
    public void Constructor_EmailType_ShouldNormalizeToLowercase()
    {
        var key = new PixKey("Pagamentos@Acme.COM", PixKeyType.Email);

        Assert.Equal("pagamentos@acme.com", key.Value);
        Assert.Equal(PixKeyType.Email, key.Type);
    }

    // PIX tipo Phone: aceita 10 ou 11 dígitos (DDD + número).
    [Fact]
    public void Constructor_PhoneType_ShouldNormalizeToDigits()
    {
        var key = new PixKey("(11) 98765-4321", PixKeyType.Phone);

        Assert.Equal("11987654321", key.Value);
    }

    // PIX tipo Random: aceita 32 caracteres alfanuméricos hex e normaliza para minúsculas.
    [Fact]
    public void Constructor_RandomType_With32HexChars_ShouldNormalizeToLowercase()
    {
        var key = new PixKey("AABBCCDDEEFF00112233445566778899", PixKeyType.Random);

        Assert.Equal("aabbccddeeff00112233445566778899", key.Value);
        Assert.Equal(32, key.Value.Length);
    }

    // PIX vazio em qualquer tipo lança AP.PIX01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyValue_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new PixKey(raw, PixKeyType.Email));

        Assert.Equal("AP.PIX01", ex.Id);
    }

    // CPF inválido no tipo Cpf lança AP.PIX02 (validação de comprimento, não check digit).
    [Fact]
    public void Constructor_CpfType_WithWrongLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new PixKey("12345", PixKeyType.Cpf));

        Assert.Equal("AP.PIX02", ex.Id);
    }

    // Email malformado no tipo Email lança AP.PIX02.
    [Fact]
    public void Constructor_EmailType_WithMalformedAddress_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new PixKey("nao-eh-email", PixKeyType.Email));

        Assert.Equal("AP.PIX02", ex.Id);
    }

    // Random com menos de 32 chars lança AP.PIX02.
    [Fact]
    public void Constructor_RandomType_WithWrongLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new PixKey("ABC123", PixKeyType.Random));

        Assert.Equal("AP.PIX02", ex.Id);
    }

    // Random com caracteres não-hex (mesmo 32 chars) lança AP.PIX02.
    [Fact]
    public void Constructor_RandomType_WithNonHexChars_ShouldThrowDomainException()
    {
        var nonHex = new string('Z', 32);

        var ex = Assert.Throws<DomainException>(() => new PixKey(nonHex, PixKeyType.Random));

        Assert.Equal("AP.PIX02", ex.Id);
    }
}
