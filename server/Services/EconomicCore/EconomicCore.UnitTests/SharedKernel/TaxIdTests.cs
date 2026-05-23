namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class TaxIdTests
{
    // Known-good Brazilian test CPF/CNPJ (check digits already verified by hand).
    private const string VALID_CPF = "52998224725";
    private const string VALID_CPF_FORMATTED = "529.982.247-25";
    private const string VALID_CPF_PARTIAL = "529 982 247 25";
    private const string INVALID_CHECK_CPF = "52998224724";
    private const string REPEATED_CPF = "11111111111";

    private const string VALID_CNPJ = "11222333000181";
    private const string VALID_CNPJ_FORMATTED = "11.222.333/0001-81";
    private const string INVALID_CHECK_CNPJ = "11222333000180";
    private const string REPEATED_CNPJ = "22222222222222";

    // CPF válido (apenas dígitos) é aceito e armazenado sanitizado.
    [Fact]
    public void Constructor_WithValidPlainCpf_ShouldStoreSanitizedValue()
    {
        var taxId = new TaxId(VALID_CPF, TaxIdKind.CPF);

        Assert.Equal(VALID_CPF, taxId.Value);
        Assert.Same(TaxIdKind.CPF, taxId.Kind);
    }

    // CPF com formatação típica (pontos e hífen) é sanitizado para apenas dígitos.
    [Theory]
    [InlineData(VALID_CPF_FORMATTED)]
    [InlineData(VALID_CPF_PARTIAL)]
    [InlineData("  529.982.247-25  ")]
    public void Constructor_WithFormattedCpf_ShouldStripNonDigits(string input)
    {
        var taxId = new TaxId(input, TaxIdKind.CPF);

        Assert.Equal(VALID_CPF, taxId.Value);
    }

    // CNPJ válido (apenas dígitos) é aceito e armazenado sanitizado.
    [Fact]
    public void Constructor_WithValidPlainCnpj_ShouldStoreSanitizedValue()
    {
        var taxId = new TaxId(VALID_CNPJ, TaxIdKind.CNPJ);

        Assert.Equal(VALID_CNPJ, taxId.Value);
        Assert.Same(TaxIdKind.CNPJ, taxId.Kind);
    }

    // CNPJ com formatação típica (pontos, barra e hífen) é sanitizado para apenas dígitos.
    [Fact]
    public void Constructor_WithFormattedCnpj_ShouldStripNonDigits()
    {
        var taxId = new TaxId(VALID_CNPJ_FORMATTED, TaxIdKind.CNPJ);

        Assert.Equal(VALID_CNPJ, taxId.Value);
    }

    // TaxIdKind null no construtor lança SHK.TAX01 - KindRequired.
    [Fact]
    public void Constructor_WithNullKind_ShouldThrowSHK_TAX01()
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(VALID_CPF, null!));

        Assert.Equal("SHK.TAX01", ex.Id);
    }

    // Value vazio, null ou whitespace lança SHK.TAX02 - InvalidFormat.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrWhitespaceValue_ShouldThrowSHK_TAX02(string? input)
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(input!, TaxIdKind.CPF));

        Assert.Equal("SHK.TAX02", ex.Id);
    }

    // Length errado após sanitização (não bate com ExpectedLength do Kind) lança SHK.TAX02.
    [Theory]
    [InlineData("5299822472", TaxIdKindEnum.Cpf)]     // 10 digits for CPF
    [InlineData("529982247252", TaxIdKindEnum.Cpf)]    // 12 digits for CPF
    [InlineData("1122233300018", TaxIdKindEnum.Cnpj)]  // 13 digits for CNPJ
    [InlineData("112223330001812", TaxIdKindEnum.Cnpj)] // 15 digits for CNPJ
    public void Constructor_WithWrongDigitCount_ShouldThrowSHK_TAX02(string input, TaxIdKindEnum kindEnum)
    {
        var kind = kindEnum == TaxIdKindEnum.Cpf ? TaxIdKind.CPF : TaxIdKind.CNPJ;

        var ex = Assert.Throws<DomainException>(() => new TaxId(input, kind));

        Assert.Equal("SHK.TAX02", ex.Id);
    }

    // Valor com todos dígitos iguais (blacklist) é formalmente do tamanho certo mas inválido — SHK.TAX03.
    [Fact]
    public void Constructor_WithAllSameDigitsCpf_ShouldThrowSHK_TAX03()
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(REPEATED_CPF, TaxIdKind.CPF));

        Assert.Equal("SHK.TAX03", ex.Id);
    }

    // CPF com check digit incorreto (último dígito alterado) lança SHK.TAX03.
    [Fact]
    public void Constructor_WithBadCheckDigitCpf_ShouldThrowSHK_TAX03()
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(INVALID_CHECK_CPF, TaxIdKind.CPF));

        Assert.Equal("SHK.TAX03", ex.Id);
    }

    // CNPJ com check digit incorreto lança SHK.TAX03.
    [Fact]
    public void Constructor_WithBadCheckDigitCnpj_ShouldThrowSHK_TAX03()
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(INVALID_CHECK_CNPJ, TaxIdKind.CNPJ));

        Assert.Equal("SHK.TAX03", ex.Id);
    }

    // CNPJ no tamanho de CNPJ mas com todos dígitos iguais (ex.: 14 caracteres de "2") é rejeitado por SHK.TAX02
    // (porque o length de 14 chars contém só 14 dígitos depois da sanitização, então cai no blacklist via TAX03).
    // Aqui o valor tem 14 chars; depois de sanitizado tem 14 dígitos, então passa pelo length e bate no blacklist.
    [Fact]
    public void Constructor_WithAllSameDigitsCnpj_ShouldThrowSHK_TAX03()
    {
        var ex = Assert.Throws<DomainException>(() => new TaxId(REPEATED_CNPJ, TaxIdKind.CNPJ));

        Assert.Equal("SHK.TAX03", ex.Id);
    }

    // Formatted para CPF aplica máscara 000.000.000-00.
    [Fact]
    public void Formatted_ForCpf_ShouldApplyBrazilianMask()
    {
        var taxId = new TaxId(VALID_CPF, TaxIdKind.CPF);

        Assert.Equal(VALID_CPF_FORMATTED, taxId.Formatted());
    }

    // Formatted para CNPJ aplica máscara 00.000.000/0000-00.
    [Fact]
    public void Formatted_ForCnpj_ShouldApplyBrazilianMask()
    {
        var taxId = new TaxId(VALID_CNPJ, TaxIdKind.CNPJ);

        Assert.Equal(VALID_CNPJ_FORMATTED, taxId.Formatted());
    }

    // Dois TaxId com mesmo Value sanitizado e Kind são iguais (igualdade estrutural).
    [Fact]
    public void Equals_SameValueAndKind_ShouldBeTrue()
    {
        var a = new TaxId(VALID_CPF, TaxIdKind.CPF);
        var b = new TaxId(VALID_CPF_FORMATTED, TaxIdKind.CPF);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // TaxIds de Kinds diferentes (CPF vs CNPJ) não são iguais mesmo com Values diferentes.
    [Fact]
    public void Equals_DifferentKind_ShouldBeFalse()
    {
        var cpf = new TaxId(VALID_CPF, TaxIdKind.CPF);
        var cnpj = new TaxId(VALID_CNPJ, TaxIdKind.CNPJ);

        Assert.NotEqual(cpf, cnpj);
    }

    // Helper enum to parameterize TaxIdKind in [Theory] (Smart Enum instances are not InlineData-compatible).
    public enum TaxIdKindEnum
    {
        Cpf,
        Cnpj
    }
}
