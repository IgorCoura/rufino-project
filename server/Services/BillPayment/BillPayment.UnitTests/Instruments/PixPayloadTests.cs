namespace BillPayment.UnitTests.Instruments;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Os payloads são <strong>sintéticos</strong>, montados com os tamanhos TLV calculados e o
/// CRC correto. BR Code real é instrumento de pagamento e não entra no repositório.
/// </summary>
public class PixPayloadTests
{
    private const string WithAmount =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    private const string WithoutAmount =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia5204000053039865802BR5912SABESP TESTE6009SAO PAULO62070503***6304AF33";

    private const string Dynamic =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private const string ForeignCurrency =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia520400005303840540510.005802BR5912SABESP TESTE6009SAO PAULO63049971";

    private const string NotPix =
        "00020126270016com.outroarranjo0103abc5204000053039865802BR5913OUTRO ARRANJO6009SAO PAULO6304EFB2";

    // Vetor de teste publicado do CRC-16/CCITT-FALSE. É o que prova o algoritmo em si —
    // conferir contra um exemplo de BR Code só provaria que dois erros combinam.
    [Fact]
    public void ComputeCrc_WithTheStandardCheckVector_ShouldMatchThePublishedValue()
    {
        Assert.Equal("29B1", PixPayload.ComputeCrc("123456789"));
    }

    // QR estático com valor fixo entrega chave, valor, nome e identificador de transação.
    [Fact]
    public void Parse_WithStaticQrCarryingAmount_ShouldExposeEveryFieldTheChecksUse()
    {
        var parsed = PixPayload.Parse(WithAmount);

        Assert.False(parsed.IsDynamic);
        Assert.Equal("11222333000181", parsed.PixKey);
        Assert.Equal(1500.00m, parsed.Amount!.Amount);
        Assert.Equal("BRL", parsed.Amount.Currency.Name);
        Assert.Equal("SABESP TESTE", parsed.MerchantName);
        Assert.Equal("SAO PAULO", parsed.MerchantCity);
        Assert.Equal("TXID0001", parsed.TransactionId);
        Assert.Null(parsed.Url);
    }

    // QR sem o campo 54 aceita qualquer quantia — o check de valor sai Skipped, não reprovado.
    [Fact]
    public void Parse_WithStaticQrWithoutAmount_ShouldLeaveAmountNull()
    {
        var parsed = PixPayload.Parse(WithoutAmount);

        Assert.Null(parsed.Amount);
        Assert.Equal("11222333000181", parsed.PixKey);
    }

    // QR dinâmico carrega URL em vez de chave: os dados reais da cobrança vêm da consulta ao PSP.
    [Fact]
    public void Parse_WithDynamicQr_ShouldExposeTheUrlAndNoKey()
    {
        var parsed = PixPayload.Parse(Dynamic);

        Assert.True(parsed.IsDynamic);
        Assert.Equal("pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca25", parsed.Url);
        Assert.Null(parsed.PixKey);
        Assert.Null(parsed.Amount);
        Assert.Equal("EDP TESTE SA", parsed.MerchantName);
    }

    // Payload ausente é recusado — BLP.PIX01.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithBlankInput_ShouldThrow_BLP_PIX01(string? input)
    {
        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse(input!));

        Assert.Equal("BLP.PIX01", ex.Id);
    }

    // Alterar um dígito do valor quebra o CRC — é a defesa contra QR adulterado.
    [Fact]
    public void Parse_WithTamperedAmount_ShouldThrow_BLP_PIX03()
    {
        var tampered = WithAmount.Replace("54071500.00", "54079500.00", StringComparison.Ordinal);

        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse(tampered));

        Assert.Equal("BLP.PIX03", ex.Id);
    }

    // QR copiado pela metade é o erro mais comum de "copia e cola" e não pode virar pagamento.
    [Fact]
    public void Parse_WithTruncatedPayload_ShouldThrow()
    {
        var truncated = WithAmount[..(WithAmount.Length / 2)];

        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse(truncated));

        Assert.StartsWith("BLP.PIX", ex.Id, StringComparison.Ordinal);
    }

    // Texto curto demais para conter sequer o campo de CRC — BLP.PIX02.
    [Fact]
    public void Parse_WithInputShorterThanTheCrcField_ShouldThrow_BLP_PIX02()
    {
        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse("6304"));

        Assert.Equal("BLP.PIX02", ex.Id);
    }

    // QR de outro arranjo de pagamento não é Pix — BLP.PIX04.
    [Fact]
    public void Parse_WithNonPixMerchantAccount_ShouldThrow_BLP_PIX04()
    {
        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse(NotPix));

        Assert.Equal("BLP.PIX04", ex.Id);
    }

    // Valor fixo em moeda estrangeira não é pagável pelos trilhos deste BC — BLP.PIX02.
    [Fact]
    public void Parse_WithForeignCurrencyAmount_ShouldThrow_BLP_PIX02()
    {
        var ex = Assert.Throws<DomainException>(() => PixPayload.Parse(ForeignCurrency));

        Assert.Equal("BLP.PIX02", ex.Id);
    }

    // TryParse é o caminho para varredura de documento, onde a maioria dos candidatos é lixo.
    [Fact]
    public void TryParse_WithGarbage_ShouldReturnFalseWithoutThrowing()
    {
        var succeeded = PixPayload.TryParse("isto nao e um br code", out var parsed);

        Assert.False(succeeded);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParse_WithValidPayload_ShouldReturnTheParsedPayload()
    {
        var succeeded = PixPayload.TryParse(WithAmount, out var parsed);

        Assert.True(succeeded);
        Assert.Equal("11222333000181", parsed!.PixKey);
    }

    // O CRC é aceito em qualquer caixa: emissores divergem entre maiúscula e minúscula.
    [Fact]
    public void Parse_WithLowercaseCrc_ShouldBeAccepted()
    {
        var lowercased = string.Concat(WithAmount.AsSpan(0, WithAmount.Length - 4), WithAmount[^4..].ToLowerInvariant());

        var parsed = PixPayload.Parse(lowercased);

        Assert.Equal("11222333000181", parsed.PixKey);
    }

    // Igualdade é pelo payload — é ele que identifica o instrumento de pagamento.
    [Fact]
    public void Equals_WithSamePayload_ShouldBeEqual()
    {
        Assert.Equal(PixPayload.Parse(WithAmount), PixPayload.Parse(WithAmount));
        Assert.NotEqual(PixPayload.Parse(WithAmount), PixPayload.Parse(WithoutAmount));
    }
}
