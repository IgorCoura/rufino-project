namespace BillPayment.UnitTests.Extraction;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.PayerProfiles.Mothers;

/// <summary>
/// O desfecho da cascata de extração, e a fusão dos documentos fiscais achados por degraus
/// diferentes.
/// </summary>
/// <remarks>
/// <strong>O instrumento e o pagador não vêm necessariamente do mesmo degrau.</strong> A guia do
/// FGTS Digital é o caso que expôs isso: o QR Pix resolve o instrumento e a camada de texto do
/// PDF é ilegível por construção, então o pagador só existe na leitura por visão. Até 2026-09-10
/// ele era descartado justamente quando havia instrumento, e a guia caía na fila de
/// reivindicação com o CNPJ do tenant impresso na página.
/// </remarks>
public class ExtractionResultTests
{
    private const string OwnCnpj = PayerProfileMother.HeadquartersCnpj;
    private const string PayeeCnpj = PayerProfileMother.ForeignCnpj;

    // A regressão que motivou o método: resolvido pelo determinístico E com pagador só na visão.
    [Fact]
    public void WithParties_ShouldAddTheCandidateToAResolvedExtraction()
    {
        var merged = Resolved().WithParties([Party(OwnCnpj)]);

        Assert.True(merged.Resolved);
        Assert.Equal(OwnCnpj, Assert.Single(merged.Parties).TaxId.Value);
    }

    // Acrescentar, nunca substituir: o que a varredura determinística leu continua valendo, e é
    // ele que sustenta o degrau 1 negativo.
    [Fact]
    public void WithParties_ShouldKeepWhatTheDeterministicScanHadAlreadyRead()
    {
        var merged = Resolved(Party(PayeeCnpj, underPayerLabel: true))
            .WithParties([Party(OwnCnpj)]);

        Assert.Equal(2, merged.Parties.Count);
        Assert.Equal(PayeeCnpj, merged.Parties[0].TaxId.Value);
        Assert.True(merged.Parties[0].UnderPayerLabel);
        Assert.Equal(OwnCnpj, merged.Parties[1].TaxId.Value);
    }

    // O degrau que resolveu e o campo que abriu o PDF são métrica da cascata: fundir documento
    // fiscal não pode reescrever de onde veio o instrumento.
    [Fact]
    public void WithParties_ShouldPreserveTheMethodAndTheUnlockedField()
    {
        var merged = Resolved(unlockedBy: "cnpj_first_5_primary").WithParties([Party(OwnCnpj)]);

        Assert.Same(ExtractionMethod.QrCode, merged.Method);
        Assert.Equal("cnpj_first_5_primary", merged.UnlockedBy);
    }

    // Deduplica por (documento, rótulo) — a mesma regra do TaxIdScanner. O mesmo número lido
    // pelos dois caminhos é uma ocorrência só.
    [Fact]
    public void WithParties_WithACandidateAlreadyPresent_ShouldNotDuplicateIt()
    {
        var extraction = Resolved(Party(OwnCnpj));

        Assert.Same(extraction, extraction.WithParties([Party(OwnCnpj)]));
    }

    // Mas o rótulo faz parte da chave: o mesmo CNPJ sob rótulo de pagador é evidência diferente
    // do mesmo CNPJ solto no rodapé, e é a ocorrência rotulada que autoriza a negativa.
    [Fact]
    public void WithParties_WithTheSameDocumentUnderADifferentLabel_ShouldKeepBoth()
    {
        var merged = Resolved(Party(OwnCnpj))
            .WithParties([Party(OwnCnpj, underPayerLabel: true)]);

        Assert.Equal(2, merged.Parties.Count);
    }

    [Fact]
    public void WithParties_WithNothingToAdd_ShouldReturnTheSameInstance()
    {
        var extraction = Resolved();

        Assert.Same(extraction, extraction.WithParties(null));
        Assert.Same(extraction, extraction.WithParties([]));
    }

    // Não resolvido também aceita candidato, e o motivo de não ter resolvido sobrevive — é ele
    // que distingue "não é boleto" de "PDF cifrado", e a triagem lê os dois de forma diferente.
    [Fact]
    public void WithParties_OverAnUnresolvedExtraction_ShouldPreserveTheReason()
    {
        var merged = ExtractionResult.Locked().WithParties([Party(OwnCnpj)]);

        Assert.True(merged.IsLocked);
        Assert.False(merged.Resolved);
        Assert.Single(merged.Parties);
    }

    /// <summary>
    /// O que a guia do FGTS Digital entrega: instrumento vindo do QR Pix e nenhum documento
    /// fiscal, porque a camada de texto do PDF não é legível.
    /// </summary>
    private static ExtractionResult Resolved(PartyCandidate? party = null, string? unlockedBy = null)
        => ExtractionResult.Found(
            [InstrumentSamples.DynamicPixQr()],
            ExtractionMethod.QrCode,
            unlockedBy,
            party is null ? null : [party]);

    private static PartyCandidate Party(string taxId, bool underPayerLabel = false)
        => PartyCandidate.TryCreate(taxId, underPayerLabel)!;
}
