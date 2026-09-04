namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.PayerProfiles.Mothers;

/// <summary>
/// A escada de roteamento: de quem é o boleto que acabou de ser extraído.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a peça que impede um usuário de pagar a conta de outro.</strong> Numa caixa
/// compartilhada chega a conta dos dois tenants, e não existe atribuição por default ao dono da
/// fonte — quando a escada não sabe, ela diz que não sabe.
/// </para>
/// <para>
/// <strong>A assimetria entre atribuir e recusar é o que mais importa aqui</strong>, e vem da
/// medição de 2026-08-12: o documento fiscal do tenant aparece em 93,3% dos boletos reais, mas
/// só 66,8% das ocorrências têm rótulo de pagador por perto. Atribuir sem rótulo é seguro
/// (casou com o cadastro do próprio tenant); recusar sem rótulo não é, porque o número tanto
/// pode ser o do pagador quanto o da concessionária.
/// </para>
/// </remarks>
public class BillRoutingServiceTests
{
    private const string OwnCnpj = PayerProfileMother.HeadquartersCnpj;
    private const string BranchCnpj = PayerProfileMother.BranchCnpj;
    private const string SomeoneElsesCnpj = PayerProfileMother.ForeignCnpj;

    /// <summary>O CNPJ do beneficiário, que está impresso em todo boleto ao lado do do pagador.</summary>
    private const string PayeeCnpj = "11444777000161";

    private static readonly TaxId[] NoExclusivePayees = [];

    // Degrau 0: abrir o PDF com senha derivada do documento do tenant é prova de propriedade —
    // o emissor derivou aquela senha do documento de quem ele estava cobrando.
    [Fact]
    public void Route_WhenThePdfWasUnlockedByADerivedPassword_ShouldPromoteAsStrong()
    {
        var decision = BillRoutingService.Route(
            Extraction(unlockedBy: "cnpj_first_5_primary"),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
        Assert.Equal(BillRoutingService.REASON_PASSWORD_DERIVED, decision.Reason);
    }

    // O degrau 0 vence mesmo sem perfil cadastrado no contexto: a senha já saiu do cadastro
    // deste tenant quando as candidatas foram derivadas.
    [Fact]
    public void Route_WhenUnlockedByPassword_ShouldNotDependOnTheProfileBeingPassed()
    {
        var decision = BillRoutingService.Route(
            Extraction(unlockedBy: "cpf_first_5_additional_1"), profile: null, NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
    }

    // Degrau 1, o que cobre 93,3% do corpus: o documento fiscal do tenant impresso no artefato.
    [Fact]
    public void Route_WhenTheDocumentCarriesTheTenantTaxId_ShouldPromoteAsStrong()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(OwnCnpj)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
        Assert.Equal(BillRoutingService.REASON_PAYER_TAX_ID, decision.Reason);
        Assert.Equal(OwnCnpj, decision.PayerTaxId!.Value);
    }

    // O documento do tenant não precisa de rótulo para atribuir: casar com o cadastro do próprio
    // tenant já é evidência suficiente, e exigir rótulo custaria 31 pontos de cobertura.
    [Fact]
    public void Route_WhenTheTenantTaxIdHasNoLabel_ShouldStillPromote()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayeeCnpj), Party(OwnCnpj, underPayerLabel: false)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
    }

    // Filial cujo boleto chega sem cadastro prévio: a raiz do CNPJ casa quando o tenant pediu.
    [Fact]
    public void Route_WhenTheTaxIdSharesTheCnpjRootAndMatchingIsOn_ShouldPromote()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(BranchCnpj)]),
            PayerProfileMother.CompanyWithRootMatching(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
    }

    // E não casa quando o tenant não pediu: ligar a raiz por default faria duas empresas do mesmo
    // grupo econômico — que são tenants distintos — passarem uma pela outra.
    [Fact]
    public void Route_WhenTheTaxIdSharesTheRootButMatchingIsOff_ShouldNotPromote()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(BranchCnpj)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.NotSame(RoutingOutcome.Promote, decision.Outcome);
    }

    // Degrau 1 negativo: documento de terceiro SOB RÓTULO de pagador é a única evidência que
    // autoriza concluir que o boleto é de outra pessoa.
    [Fact]
    public void Route_WhenALabelledPayerTaxIdIsNotTheTenant_ShouldMarkAsForeign()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(SomeoneElsesCnpj, underPayerLabel: true)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Foreign, decision.Outcome);
        Assert.Equal(BillRoutingService.REASON_PAYER_IS_ANOTHER, decision.Reason);
        Assert.Null(decision.Confidence);
    }

    // TESTE ÂNCORA DA ASSIMETRIA. Todo boleto traz o CNPJ do BENEFICIÁRIO impresso, e ele não é
    // do tenant. Se um documento sem rótulo bastasse para concluir "é de outra pessoa", toda
    // conta de concessionária iria para a quarentena cega — onde o usuário NÃO pode reivindicar.
    // Sem rótulo, o desfecho tem que ser Unrouted.
    [Fact]
    public void Route_WhenAnUnlabelledTaxIdIsNotTheTenant_ShouldBeUnroutedAndNotForeign()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayeeCnpj, underPayerLabel: false)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Unrouted, decision.Outcome);
        Assert.Equal(BillRoutingService.REASON_PAYER_NOT_IDENTIFIED, decision.Reason);
    }

    // Degrau 3: o beneficiário é cadastrado só por este tenant. Reduz fila, e por isso a
    // confiança é Weak — a aprovação humana continua obrigatória.
    [Fact]
    public void Route_WhenThePayeeIsExclusiveToTheTenant_ShouldPromoteAsWeak()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayeeCnpj)]),
            PayerProfileMother.Register(),
            [new TaxId(PayeeCnpj, TaxIdKind.CNPJ)]);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Weak, decision.Confidence);
        Assert.Equal(BillRoutingService.REASON_EXCLUSIVE_PAYEE, decision.Reason);

        // Beneficiário exclusivo NÃO é pagador extraído: preencher o campo com o CNPJ do credor
        // faria o check PayerMatch reprovar o boleto por contradizer o cadastro.
        Assert.Null(decision.PayerTaxId);
    }

    // A ordem do doc 07 não pode inverter: o degrau 3 nunca sobrepõe um ForeignPayer do degrau 1,
    // porque o beneficiário exclusivo é indício e o pagador rotulado é constatação.
    [Fact]
    public void Route_WhenTheresBothALabelledForeignPayerAndAnExclusivePayee_ShouldMarkAsForeign()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(SomeoneElsesCnpj, underPayerLabel: true), Party(PayeeCnpj)]),
            PayerProfileMother.Register(),
            [new TaxId(PayeeCnpj, TaxIdKind.CNPJ)]);

        Assert.Same(RoutingOutcome.Foreign, decision.Outcome);
    }

    // Beneficiário que dois tenants cadastraram não entra na lista de exclusivos, e aí não há
    // degrau 3 — a evidência é ambígua e escolher seria adivinhar de quem é a conta.
    [Fact]
    public void Route_WhenThePayeeIsSharedWithAnotherTenant_ShouldBeUnrouted()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayeeCnpj)]),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Unrouted, decision.Outcome);
    }

    // Degrau 4: artefato que não diz nada sobre quem paga vai para a fila de reivindicação.
    [Fact]
    public void Route_WhenTheDocumentCarriesNoTaxId_ShouldBeUnrouted()
    {
        var decision = BillRoutingService.Route(
            Extraction(), PayerProfileMother.Register(), NoExclusivePayees);

        Assert.Same(RoutingOutcome.Unrouted, decision.Outcome);
        Assert.Null(decision.Confidence);
    }

    // Tenant sem cadastro fiscal não quebra a escada: sem PayerProfile não há contra o quê
    // comparar, e o item cai para a reivindicação em vez de ser atribuído por falta de opção.
    [Fact]
    public void Route_WhenTheTenantHasNoPayerProfile_ShouldBeUnrouted()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(OwnCnpj)]), profile: null, NoExclusivePayees);

        Assert.Same(RoutingOutcome.Unrouted, decision.Outcome);
    }

    // PF é tratada pelo mesmo caminho de PJ — não existe if de pessoa física na escada (doc 07).
    [Fact]
    public void Route_WhenTheDocumentCarriesTheIndividualCpf_ShouldPromoteAsStrong()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayerProfileMother.DefaultCpf)]),
            PayerProfileMother.Individual(),
            NoExclusivePayees);

        Assert.Same(RoutingOutcome.Promote, decision.Outcome);
        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
    }

    private static PartyCandidate Party(string taxId, bool underPayerLabel = false)
        => PartyCandidate.TryCreate(taxId, underPayerLabel)!;

    /// <summary>
    /// A escada só roda sobre artefato que já tem instrumento válido, então o instrumento entra
    /// sempre — ele não é o assunto de nenhum destes testes.
    /// </summary>
    private static ExtractionResult Extraction(
        IEnumerable<PartyCandidate>? parties = null,
        string? unlockedBy = null)
        => ExtractionResult.Found(
            [InstrumentSamples.Barcode()], ExtractionMethod.EmbeddedText, unlockedBy, parties);

    // REGRESSÃO (2026-08-26, boleto BBZ-COND): o degrau 0 respondia "é sua conta" sem informar
    // QUAL documento — e como ele é um return antecipado, o degrau 1, que informaria, nunca
    // rodava. O boleto nascia sem pagador e o check PayerMatch dizia "o documento não traz o
    // documento fiscal do pagador" sobre um PDF que trazia.
    [Fact]
    public void Route_WhenUnlockedByPasswordAndTheTaxIdIsAlsoPrinted_ShouldCarryTheTaxId()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(OwnCnpj)], unlockedBy: "cnpj_first_5_primary"),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
        Assert.Equal(BillRoutingService.REASON_PASSWORD_DERIVED, decision.Reason);
        Assert.Equal(OwnCnpj, decision.PayerTaxId?.Value);
    }

    // PDF que abre por senha sem repetir o documento no corpo existe; ali não há o que informar,
    // e o degrau 0 continua valendo como prova de propriedade.
    [Fact]
    public void Route_WhenUnlockedByPasswordAndNoTaxIdIsPrinted_ShouldStillPromoteWithoutTaxId()
    {
        var decision = BillRoutingService.Route(
            Extraction(parties: [Party(PayeeCnpj)], unlockedBy: "cnpj_first_5_primary"),
            PayerProfileMother.Register(),
            NoExclusivePayees);

        Assert.Same(RoutingConfidence.Strong, decision.Confidence);
        Assert.Null(decision.PayerTaxId);
    }
}
