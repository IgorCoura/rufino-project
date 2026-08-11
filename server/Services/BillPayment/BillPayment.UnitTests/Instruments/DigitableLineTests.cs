namespace BillPayment.UnitTests.Instruments;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>
/// As linhas destes testes são <strong>sintéticas</strong>, geradas com os DVs corretos.
/// Linha digitável real é instrumento de pagamento e não entra no repositório — a suíte
/// contra o corpus real roda fora, via <c>tools/analyze-boleto-corpus.js</c>.
/// </summary>
public class DigitableLineTests
{
    // "Hoje" fixo: o fator de vencimento é ambíguo entre duas épocas e a desambiguação
    // usa proximidade com esta data. Relógio real deixaria o teste flaky em 2050.
    private static readonly DateTime Today = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    private const string BankSlip341 = "34191234546789012345767890123457314880000061507";
    private const string BankSlip033 = "03399876534321098765743210987657414930000140980";
    private const string BankSlipWithoutDueDate = "23791111112222233333244444555559900000000030575";
    private const string UtilityMod10 = "826600000010224812345672890123456786901234567898";
    private const string UtilityMod11 = "858200001496473098765435210987654322109876543212";
    private const string BankSlipWithZeroBank = "00091234566789012345767890123457714880000061507";

    // Cobrança traz o COMPE nas posições 1–3 do código de barras — a fonte do check de banco.
    [Theory]
    [InlineData(BankSlip341, "341")]
    [InlineData(BankSlip033, "033")]
    public void Parse_WithBankSlip_ShouldExposeTheSettlingBankFromTheBarcode(string line, string expectedBank)
    {
        var parsed = DigitableLine.Parse(line, Today);

        Assert.Same(BillKind.BankSlip, parsed.Kind);
        Assert.Equal(expectedBank, parsed.BankCode.Value);
    }

    // O código de barras tem 44 posições e é remontado a partir da linha, não recortado dela.
    [Fact]
    public void Parse_WithBankSlip_ShouldRebuildA44DigitBarcode()
    {
        var parsed = DigitableLine.Parse(BankSlip341, Today);

        Assert.Equal(44, parsed.Barcode.Length);
        Assert.StartsWith("3419", parsed.Barcode, StringComparison.Ordinal);
    }

    // O valor sai do código de barras em centavos e vira Money em BRL.
    [Theory]
    [InlineData(BankSlip341, 615.07)]
    [InlineData(BankSlip033, 1409.80)]
    [InlineData(UtilityMod10, 122.48)]
    [InlineData(UtilityMod11, 14947.30)]
    public void Parse_ShouldReadTheAmountInReais(string line, double expected)
    {
        var parsed = DigitableLine.Parse(line, Today);

        Assert.Equal((decimal)expected, parsed.Amount.Amount);
        Assert.Equal("BRL", parsed.Amount.Currency.Name);
    }

    // O fator de vencimento vira data pela época mais próxima de "hoje" — aqui, a de 2025.
    [Theory]
    [InlineData(BankSlip341, "2026-06-25")]
    [InlineData(BankSlip033, "2026-06-30")]
    public void Parse_WithDueDateFactor_ShouldResolveToTheEpochNearestToday(string line, string expected)
    {
        var parsed = DigitableLine.Parse(line, Today);

        Assert.Equal(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), parsed.DueDate);
    }

    // Fator zero significa "sem vencimento" e não pode virar 08/10/1997.
    [Fact]
    public void Parse_WithZeroDueDateFactor_ShouldLeaveDueDateNull()
    {
        var parsed = DigitableLine.Parse(BankSlipWithoutDueDate, Today);

        Assert.Null(parsed.DueDate);
    }

    // Arrecadação é reconhecida pelo 8 inicial e pelos 48 dígitos, nos dois algoritmos de DV.
    [Theory]
    [InlineData(UtilityMod10)]
    [InlineData(UtilityMod11)]
    public void Parse_WithUtilityLine_ShouldBeRecognizedAsUtility(string line)
    {
        var parsed = DigitableLine.Parse(line, Today);

        Assert.Same(BillKind.Utility, parsed.Kind);
        Assert.False(parsed.Kind.CarriesBankCode);
    }

    // Arrecadação não carrega banco em posição nenhuma; pedir o banco é erro de programação — BLP.DGL06.
    [Fact]
    public void BankCode_OnUtilityLine_ShouldThrow_BLP_DGL06()
    {
        var parsed = DigitableLine.Parse(UtilityMod10, Today);

        var ex = Assert.Throws<DomainException>(() => parsed.BankCode);

        Assert.Equal("BLP.DGL06", ex.Id);
    }

    // Arrecadação também não carrega fator de vencimento — a data vive no corpo do documento.
    [Fact]
    public void Parse_WithUtilityLine_ShouldLeaveDueDateNull()
    {
        var parsed = DigitableLine.Parse(UtilityMod10, Today);

        Assert.Null(parsed.DueDate);
    }

    // A entrada é aceita como o usuário digita — pontos, espaços e hifens são ignorados.
    [Fact]
    public void Parse_WithFormattingCharacters_ShouldSanitizeBeforeValidating()
    {
        var formatted = "34191.23454 67890.123457 67890.123457 3 14880000061507";

        var parsed = DigitableLine.Parse(formatted, Today);

        Assert.Equal(BankSlip341, parsed.Value);
    }

    // Linha ausente é recusada — BLP.DGL01.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithBlankInput_ShouldThrow_BLP_DGL01(string? input)
    {
        var ex = Assert.Throws<DomainException>(() => DigitableLine.Parse(input!, Today));

        Assert.Equal("BLP.DGL01", ex.Id);
    }

    // Quantidade de dígitos que não é 47 nem 48 não corresponde a layout nenhum — BLP.DGL02.
    [Theory]
    [InlineData("123")]
    [InlineData("3419123454678901234576789012345731488000006150")]
    [InlineData("341912345467890123457678901234573148800000615077")]
    public void Parse_WithUnrecognizableLength_ShouldThrow_BLP_DGL02(string input)
    {
        var ex = Assert.Throws<DomainException>(() => DigitableLine.Parse(input, Today));

        Assert.Equal("BLP.DGL02", ex.Id);
    }

    // Alterar um dígito de um campo quebra o mod 10 daquele campo — BLP.DGL03.
    [Fact]
    public void Parse_WithTamperedField_ShouldThrow_BLP_DGL03()
    {
        var tampered = string.Concat("3419123455", BankSlip341.AsSpan(10));

        var ex = Assert.Throws<DomainException>(() => DigitableLine.Parse(tampered, Today));

        Assert.Equal("BLP.DGL03", ex.Id);
    }

    // Mexer no banco mantendo os DVs de campo quebra o DV geral — é o que sustenta o check 6.
    [Fact]
    public void Parse_WithTamperedBank_ShouldThrow_BLP_DGL04()
    {
        // Troca 341 por 237 e recalcula só o mod 10 do campo 1: o DV geral continua o do 341.
        var field1 = string.Concat("2379", BankSlip341.AsSpan(4, 5));
        var tampered = string.Concat(field1, Mod10(field1).ToString(System.Globalization.CultureInfo.InvariantCulture), BankSlip341.AsSpan(10));

        var ex = Assert.Throws<DomainException>(() => DigitableLine.Parse(tampered, Today));

        Assert.Equal("BLP.DGL04", ex.Id);
    }

    // Banco 000 não existe na tabela COMPE — BLP.DGL05. Regressão: no corpus real, uma janela
    // de 47 dígitos de lixo renderizado como texto passou nos quatro DVs e reportou
    // "banco=000 valor=4.411.000,00" com toda a confiança. DV não basta.
    [Fact]
    public void Parse_WithUnassignedBank_ShouldThrow_BLP_DGL05()
    {
        var ex = Assert.Throws<DomainException>(() => DigitableLine.Parse(BankSlipWithZeroBank, Today));

        Assert.Equal("BLP.DGL05", ex.Id);
    }

    // TryParse é o caminho para varredura de texto, onde a maioria dos candidatos é lixo.
    [Fact]
    public void TryParse_WithInvalidLine_ShouldReturnFalseWithoutThrowing()
    {
        var succeeded = DigitableLine.TryParse("nao e uma linha digitavel", Today, out var parsed);

        Assert.False(succeeded);
        Assert.Null(parsed);
    }

    [Fact]
    public void TryParse_WithValidLine_ShouldReturnTheParsedLine()
    {
        var succeeded = DigitableLine.TryParse(BankSlip341, Today, out var parsed);

        Assert.True(succeeded);
        Assert.Equal("341", parsed!.BankCode.Value);
    }

    // Igualdade é pelo código de barras: a mesma linha formatada de dois jeitos é o mesmo documento.
    [Fact]
    public void Equals_WithSameLineFormattedDifferently_ShouldBeEqual()
    {
        var plain = DigitableLine.Parse(BankSlip341, Today);
        var formatted = DigitableLine.Parse("34191.23454 67890.123457 67890.123457 3 14880000061507", Today);

        Assert.Equal(plain, formatted);
        Assert.Equal(plain.GetHashCode(), formatted.GetHashCode());
    }

    private static int Mod10(string digits)
    {
        var total = 0;
        var multiplier = 2;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var product = (digits[i] - '0') * multiplier;
            if (product > 9)
                product = (product / 10) + (product % 10);

            total += product;
            multiplier = multiplier == 2 ? 1 : 2;
        }

        return (10 - (total % 10)) % 10;
    }

    // Ida e volta: a linha vira codigo de barras e o codigo de barras vira a MESMA linha.
    // E o que permite ler o boleto digitalizado, onde so a barra e legivel.
    [Theory]
    [InlineData(BankSlip341)]
    [InlineData(UtilityMod10)]
    [InlineData(UtilityMod11)]
    public void FromBarcode_ShouldRebuildTheSameDocument(string original)
    {
        var line = DigitableLine.Parse(original, Today);

        var rebuilt = DigitableLine.FromBarcode(line.Barcode, Today);

        // O codigo de barras e o que identifica o documento — e a chave natural do instrumento
        // sai dele, nao da linha. Se ele bate, nao ha risco de boleto duplicado.
        Assert.Equal(line.Barcode, rebuilt.Barcode);
        Assert.Equal(line.Kind, rebuilt.Kind);
        Assert.Equal(line.Amount, rebuilt.Amount);
        Assert.Equal(line.DueDate, rebuilt.DueDate);
    }

    // Em cobranca a linha reconstruida e identica a impressa, digito a digito.
    [Fact]
    public void FromBarcode_ForBankSlip_ShouldRebuildTheExactPrintedLine()
    {
        var line = DigitableLine.Parse(BankSlip341, Today);

        Assert.Equal(line.Value, DigitableLine.FromBarcode(line.Barcode, Today).Value);
    }

    // Codigo de barras com comprimento errado e recusado — nao existe atalho para dentro do VO.
    [Theory]
    [InlineData("123")]
    [InlineData("3419123454678901234576789012345731488000006150")]
    public void FromBarcode_WithWrongLength_ShouldThrow(string barcode)
        => Assert.Throws<DomainException>(() => DigitableLine.FromBarcode(barcode, Today));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromBarcode_WithBlankInput_ShouldThrow(string? barcode)
        => Assert.Throws<DomainException>(() => DigitableLine.FromBarcode(barcode!, Today));

    // Reconstruir NAO e porta dos fundos: banco nao atribuido continua sendo recusado, porque
    // FromBarcode delega ao Parse em vez de montar o VO direto.
    [Fact]
    public void FromBarcode_WithUnassignedBank_ShouldStillBeRejected()
    {
        var valido = DigitableLine.Parse(BankSlip341, Today).Barcode;
        var comBancoZerado = "000" + valido[3..];

        Assert.Throws<DomainException>(() => DigitableLine.FromBarcode(comBancoZerado, Today));
    }

}
