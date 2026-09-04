namespace BillPayment.UnitTests.Extraction;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O documento fiscal lido do artefato, antes de alguém decidir de quem ele é.
/// </summary>
public class PartyCandidateTests
{
    private const string ValidCnpj = "11222333000181";
    private const string ValidCpf = "52998224725";

    // O dígito verificador é quem decide: a leitura propõe a sequência e o VO reprova o que não
    // fecha, mesma doutrina do CandidateScanner (ADR-011).
    [Theory]
    [InlineData("11222333000182")]  // último dígito trocado
    [InlineData("52998224726")]
    [InlineData("11111111111")]     // repetido — passa na conta e não é CPF
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    public void TryCreate_WithAnInvalidDocument_ShouldReturnNull(string? taxId)
    {
        Assert.Null(PartyCandidate.TryCreate(taxId));
    }

    // CNPJ e CPF válidos entram, e o tipo é deduzido do tamanho — 14 e 11 dígitos.
    [Fact]
    public void TryCreate_WithValidDocuments_ShouldParseBothKinds()
    {
        Assert.Same(TaxIdKind.CNPJ, PartyCandidate.TryCreate(ValidCnpj)!.TaxId.Kind);
        Assert.Same(TaxIdKind.CPF, PartyCandidate.TryCreate(ValidCpf)!.TaxId.Kind);
    }

    // O rótulo é o que separa "achei um documento" de "achei o documento do PAGADOR", e por
    // isso viaja junto do número em vez de ser recalculado depois.
    [Fact]
    public void TryCreate_ShouldCarryWhetherTheDocumentWasUnderAPayerLabel()
    {
        Assert.True(PartyCandidate.TryCreate(ValidCnpj, underPayerLabel: true)!.UnderPayerLabel);
        Assert.False(PartyCandidate.TryCreate(ValidCnpj)!.UnderPayerLabel);
    }

    // O mesmo documento rotulado e não rotulado são candidatos DIFERENTES: é a ocorrência
    // rotulada que autoriza a recusa, então colapsá-las perderia a evidência.
    [Fact]
    public void Equality_ShouldDistinguishTheLabelledOccurrenceFromTheBareOne()
    {
        Assert.NotEqual(
            PartyCandidate.TryCreate(ValidCnpj, underPayerLabel: true),
            PartyCandidate.TryCreate(ValidCnpj, underPayerLabel: false));

        Assert.Equal(
            PartyCandidate.TryCreate(ValidCnpj, underPayerLabel: true),
            PartyCandidate.TryCreate(ValidCnpj, underPayerLabel: true));
    }

    // Formatação do emissor não muda o candidato: o VO guarda só os dígitos.
    [Fact]
    public void TryCreate_WithAFormattedDocument_ShouldNormalizeToDigits()
    {
        Assert.Equal(ValidCnpj, PartyCandidate.TryCreate("11.222.333/0001-81")!.TaxId.Value);
    }
}
