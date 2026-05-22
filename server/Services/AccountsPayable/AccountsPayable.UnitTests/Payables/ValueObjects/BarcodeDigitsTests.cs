namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using System.Text;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// BarcodeDigits — código de barras de boleto, 44 dígitos, DV mod-11 na posição 5.
/// Helper <see cref="WithComputedDv"/> insere o DV correto nos 43 dígitos para casos positivos.
/// </summary>
public class BarcodeDigitsTests
{
    // Insere o DV correto na posição 5 (0-based: 4). Recebe 43 dígitos sem o DV.
    private static string WithComputedDv(string digits43)
    {
        if (digits43.Length != 43) throw new InvalidOperationException("Helper espera 43 dígitos.");

        var dv = ComputeDv(digits43);

        var sb = new StringBuilder(44);
        sb.Append(digits43.AsSpan(0, 4));
        sb.Append(dv);
        sb.Append(digits43.AsSpan(4));
        return sb.ToString();
    }

    private static int ComputeDv(string s43)
    {
        var sum = 0;
        var weight = 2;
        for (var i = s43.Length - 1; i >= 0; i--)
        {
            sum += (s43[i] - '0') * weight;
            weight++;
            if (weight > 9) weight = 2;
        }
        var remainder = sum % 11;
        var dv = 11 - remainder;
        if (dv == 0 || dv == 10 || dv == 11) dv = 1;
        return dv;
    }

    // 43 dígitos válidos (sem o DV na posição 5) com banco "001" e moeda "9". Os primeiros 4
    // chars compõem o cabeçalho (banco+moeda) e os 39 seguintes representam fator/valor/campo livre.
    private const string SAMPLE_43 = "0019020120012740420006911978140000050000000";

    // String vazia ou só com espaços lança AP.BCD01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmpty_ShouldThrow_BCD01(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new BarcodeDigits(value));
        Assert.Equal("AP.BCD01", ex.Id);
    }

    // Comprimento diferente de 44 (após remover separadores) lança AP.BCD02.
    [Theory]
    [InlineData("12345")]
    [InlineData("123456789012345678901234567890123456789012345")] // 45
    public void Constructor_InvalidLength_ShouldThrow_BCD02(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new BarcodeDigits(raw));
        Assert.Equal("AP.BCD02", ex.Id);
    }

    // Caracteres não-numéricos (fora dos separadores tolerados . - whitespace) lançam AP.BCD03.
    [Fact]
    public void Constructor_NonNumericChars_ShouldThrow_BCD03()
    {
        var raw = new string('1', 43) + "A"; // letra no fim
        var ex = Assert.Throws<DomainException>(() => new BarcodeDigits(raw));
        Assert.Equal("AP.BCD03", ex.Id);
    }

    // Código de banco "000" é rejeitado com AP.BCD05 (faixa válida começa em 001).
    [Fact]
    public void Constructor_BankCodeZero_ShouldThrow_BCD05()
    {
        var s43 = "000" + SAMPLE_43[3..];
        var full = WithComputedDv(s43);
        var ex = Assert.Throws<DomainException>(() => new BarcodeDigits(full));
        Assert.Equal("AP.BCD05", ex.Id);
    }

    // DV mod-11 errado lança AP.BCD04.
    [Fact]
    public void Constructor_WrongDigitVerifier_ShouldThrow_BCD04()
    {
        var full = WithComputedDv(SAMPLE_43);
        // Sabota o DV trocando-o por (DV+1)%10.
        var dvCorreto = full[4] - '0';
        var dvErrado = (dvCorreto + 1) % 10;
        var sabotado = full[..4] + dvErrado + full[5..];
        var ex = Assert.Throws<DomainException>(() => new BarcodeDigits(sabotado));
        Assert.Equal("AP.BCD04", ex.Id);
    }

    // Código válido constrói o VO e armazena apenas dígitos.
    [Fact]
    public void Constructor_ValidBarcode_ShouldBuildAndPreserveValue()
    {
        var full = WithComputedDv(SAMPLE_43);
        var vo = new BarcodeDigits(full);
        Assert.Equal(full, vo.Value);
        Assert.Equal(BarcodeDigits.LENGTH, vo.Value.Length);
    }

    // Separadores ponto/hífen/espaço são tolerados na entrada e removidos no armazenamento.
    [Fact]
    public void Constructor_WithSeparators_ShouldNormalize()
    {
        var full = WithComputedDv(SAMPLE_43);
        // Insere separadores arbitrários.
        var withSeparators = full[..5] + " " + full[5..10] + "." + full[10..20] + "-" + full[20..];
        var vo = new BarcodeDigits(withSeparators);
        Assert.Equal(full, vo.Value);
    }

    // Igualdade estrutural por Value.
    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var full = WithComputedDv(SAMPLE_43);
        Assert.Equal(new BarcodeDigits(full), new BarcodeDigits(full));
    }

    // ToDigitableLine deriva 47 dígitos válidos do código de barras: 5 campos com DVs mod-10 nos 3 primeiros.
    [Fact]
    public void ToDigitableLine_ShouldDeriveValid47DigitsRepresentation()
    {
        var full = WithComputedDv(SAMPLE_43);
        var barcode = new BarcodeDigits(full);

        var line = barcode.ToDigitableLine();

        Assert.Equal(DigitableLine.LENGTH, line.Value.Length);
        // Campo 4 (1 dígito na posição 32 da linha digitável, contando da esquerda: 10+11+11=32) é o DV geral do barcode.
        Assert.Equal(full[BarcodeDigits.DV_POSITION], line.Value[32]);
    }

    // ToDigitableLine seguido de leitura idempotente: o resultado tem comprimento 47 (validado pelo VO DigitableLine).
    [Fact]
    public void ToDigitableLine_ResultShouldRoundTripThroughVoConstructor()
    {
        var full = WithComputedDv(SAMPLE_43);
        var barcode = new BarcodeDigits(full);

        var line = barcode.ToDigitableLine();
        var lineAgain = new DigitableLine(line.Value);

        Assert.Equal(line, lineAgain);
    }
}
