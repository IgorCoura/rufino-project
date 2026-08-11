namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Services;
using BillPayment.UnitTests.Instruments;

/// <summary>
/// O funil que separa o que o modelo <em>propôs</em> do que o domínio aceita.
/// </summary>
/// <remarks>
/// <strong>Estes são os testes mais valiosos da 2.4.</strong> Eles provam que a extração por IA
/// não tem autoridade nenhuma: candidato inventado, com dígito trocado ou com CRC quebrado não
/// vira instrumento, e sem instrumento não há boleto (ADR-011). Se um dia alguém "simplificar" o
/// caminho e passar candidato direto para a <c>Bill</c>, são estes que quebram.
/// </remarks>
public class CandidateValidationServiceTests
{
    private static readonly DateTime Today = InstrumentSamples.Today;

    // Candidato com DV correto vira instrumento — o caminho feliz existe.
    [Fact]
    public void Validate_WithAValidDigitableLine_ShouldProduceAnInstrument()
    {
        var document = ExtractedDocument.From(digitableLineCandidates: [InstrumentSamples.BankSlipLine341]);

        var instruments = CandidateValidationService.Validate(document, Today);

        Assert.Equal(PaymentInstrumentKind.Barcode, Assert.Single(instruments).Kind);
    }

    // Teste âncora do ADR-011: linha ALUCINADA — um dígito trocado numa linha real — é barrada
    // pelo dígito verificador e não produz instrumento nenhum.
    [Fact]
    public void Validate_WithAHallucinatedDigitableLine_ShouldProduceNothing()
    {
        var adulterada = Tamper(InstrumentSamples.BankSlipLine341);
        var document = ExtractedDocument.From(digitableLineCandidates: [adulterada]);

        var instruments = CandidateValidationService.Validate(document, Today);

        Assert.Empty(instruments);
    }

    // Número inventado do zero também não passa: o DV é prova de forma, e lixo não a satisfaz.
    [Theory]
    [InlineData("12345678901234567890123456789012345678901234567")]
    [InlineData("00000000000000000000000000000000000000000000000")]
    [InlineData("nao sei dizer")]
    [InlineData("")]
    public void Validate_WithGarbageCandidates_ShouldProduceNothing(string candidate)
    {
        var document = ExtractedDocument.From(digitableLineCandidates: [candidate]);

        Assert.Empty(CandidateValidationService.Validate(document, Today));
    }

    // O modelo devolve o número como está IMPRESSO, com pontos e espaços — só os dígitos contam.
    [Fact]
    public void Validate_WithAFormattedDigitableLine_ShouldStillAccept()
    {
        var formatada = $"{InstrumentSamples.BankSlipLine341[..5]}.{InstrumentSamples.BankSlipLine341[5..10]} " +
                        $"{InstrumentSamples.BankSlipLine341[10..]}";

        var document = ExtractedDocument.From(digitableLineCandidates: [formatada]);

        Assert.Single(CandidateValidationService.Validate(document, Today));
    }

    // Quando só há o código de barras impresso (44 dígitos), a linha é reconstruída e revalidada.
    [Fact]
    public void Validate_WithA44DigitBarcode_ShouldReconstructTheLine()
    {
        var barcode = PaymentInstrument.FromBarcode(
            DigitableLine.Parse(InstrumentSamples.BankSlipLine341, Today)).NaturalKey;

        var document = ExtractedDocument.From(digitableLineCandidates: [barcode]);

        Assert.Single(CandidateValidationService.Validate(document, Today));
    }

    // BR Code com CRC correto passa; adulterado não — a mesma prova, no trilho Pix.
    [Fact]
    public void Validate_WithPixPayloads_ShouldAcceptOnlyTheOneWithAValidCrc()
    {
        var document = ExtractedDocument.From(
            pixPayloadCandidates:
            [
                InstrumentSamples.StaticPixWithAmount,
                InstrumentSamples.StaticPixWithAmount[..^4] + "0000",
            ]);

        Assert.Single(CandidateValidationService.Validate(document, Today));
    }

    // Bom e ruim na mesma resposta: o ruim é descartado sem levar o bom junto — que é o que
    // permite pedir ao modelo duas leituras quando ele está em dúvida entre dois dígitos.
    [Fact]
    public void Validate_WithMixedCandidates_ShouldKeepOnlyTheValidOnes()
    {
        var document = ExtractedDocument.From(
            digitableLineCandidates: [Tamper(InstrumentSamples.BankSlipLine341), InstrumentSamples.BankSlipLine341],
            pixPayloadCandidates: ["000201lixo", InstrumentSamples.DynamicPix]);

        var instruments = CandidateValidationService.Validate(document, Today);

        Assert.Equal(2, instruments.Count);
    }

    // O mesmo boleto lido pelo texto e pela visão não vira dois instrumentos: o conjunto de
    // chaves já vistas é compartilhado com os degraus anteriores da cascata.
    [Fact]
    public void Validate_WhenTheInstrumentWasAlreadySeen_ShouldNotRepeatIt()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { InstrumentSamples.Barcode().NaturalKey };
        var document = ExtractedDocument.From(digitableLineCandidates: [InstrumentSamples.BankSlipLine341]);

        Assert.Empty(CandidateValidationService.Validate(document, Today, seen));
    }

    // Guia de arrecadação (48 dígitos, começa com 8) segue o outro esquema de DV e também passa.
    [Fact]
    public void Validate_WithAUtilityLine_ShouldProduceAnInstrument()
    {
        var document = ExtractedDocument.From(digitableLineCandidates: [InstrumentSamples.UtilityLine]);

        Assert.Single(CandidateValidationService.Validate(document, Today));
    }

    /// <summary>Troca um dígito do meio — a alucinação típica, que "parece" a linha certa.</summary>
    private static string Tamper(string line)
    {
        var digits = line.ToCharArray();
        var middle = digits.Length / 2;
        digits[middle] = digits[middle] == '9' ? '8' : (char)(digits[middle] + 1);

        return new string(digits);
    }
}
