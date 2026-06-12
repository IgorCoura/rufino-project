namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractLatePaymentTests
{
    private static readonly EconomicEventId CoveringEventId =
        EconomicEventId.From(new Guid("77777777-7777-7777-8777-777777777777"));

    private static Func<CommitmentId> SequentialFactory()
    {
        var counter = 0;
        return () =>
        {
            counter++;
            var bytes = new byte[16];
            BitConverter.GetBytes(counter).CopyTo(bytes, 0);
            return CommitmentId.From(new Guid(bytes));
        };
    }

    // Fábrica de ids para a trilha Penalty, com offset alto para não colidir com os ids da ativação.
    private static Func<CommitmentId> PenaltyFactory()
    {
        var counter = 1000;
        return () =>
        {
            counter++;
            var bytes = new byte[16];
            BitConverter.GetBytes(counter).CopyTo(bytes, 0);
            return CommitmentId.From(new Guid(bytes));
        };
    }

    private static (EconomicContract Contract, CommitmentId RentOutflow) ActivatedOneMonth()
    {
        // DefaultTerms: anchor 5, window 15 dias → janela [2025-10-05, 2025-10-20].
        var contract = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();
        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());
        var rentOutflow = contract.FindPromisedCommitment(
            EconomicContractMother.October2025(), CommitmentDirection.OutflowPromise, CommitmentPurpose.Rent);
        return (contract, rentOutflow.Id);
    }

    // Pagamento one-shot após a janela materializa o par Penalty (Promised) e retorna o settlement com base 1000 + multa 20 (2%).
    [Fact]
    public void MaterializeLatePaymentPenalty_PaidAfterWindow_ShouldCreatePenaltyPairAndReturnSettlement()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var settlement = contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc), 1020m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.Equal(rentOutflow, settlement.BaseCommitmentId);
        Assert.Equal(1000m, settlement.BaseAmountValue);
        Assert.Equal(20m, settlement.PenaltyAmountValue);
        Assert.True(settlement.PenaltyMaterialized);
        var penaltyCommitments = contract.Commitments.Where(c => c.Purpose == CommitmentPurpose.Penalty).ToList();
        Assert.Equal(2, penaltyCommitments.Count);
        Assert.All(penaltyCommitments, c => Assert.Equal(CommitmentStatus.Promised, c.Status));
        var penaltyOutflow = penaltyCommitments.Single(c => c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(settlement.PenaltyCommitmentId, penaltyOutflow.Id);
        Assert.Equal(20m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Meses cheios de atraso somam juros de mora: pago em dez/2025 (2 meses após out) → multa 1000 × (2% + 1%×2) = 40, total 1040.
    [Fact]
    public void MaterializeLatePaymentPenalty_WithFullMonthsLate_ShouldAddInterest()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var settlement = contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 12, 10, 12, 0, 0, DateTimeKind.Utc), 1040m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.Equal(40m, settlement.PenaltyAmountValue);
    }

    // Total informado diferente de base + penalidade lança ECC.CTR45 sem materializar nada (validação antes da mutação).
    [Fact]
    public void MaterializeLatePaymentPenalty_WithWrongTotal_ShouldThrowECC_CTR45_WithoutMutation()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var ex = Assert.Throws<DomainException>(() => contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc), 1030m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR45", ex.Id);
        Assert.DoesNotContain(contract.Commitments, c => c.Purpose == CommitmentPurpose.Penalty);
    }

    // Pagamento dentro da janela de cumprimento não caracteriza atraso e lança ECC.CTR46 (fluxo normal deve ser usado).
    [Fact]
    public void MaterializeLatePaymentPenalty_PaidWithinWindow_ShouldThrowECC_CTR46()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var ex = Assert.Throws<DomainException>(() => contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 10, 18, 12, 0, 0, DateTimeKind.Utc), 1020m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR46", ex.Id);
    }

    // Apontar o pagamento one-shot para um commitment da própria trilha Penalty lança ECC.CTR47 (alvo deve ser a trilha base).
    [Fact]
    public void MaterializeLatePaymentPenalty_OnPenaltyTrack_ShouldThrowECC_CTR47()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);
        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);

        var ex = Assert.Throws<DomainException>(() => contract.MaterializeLatePaymentPenalty(
            penaltyOutflow.Id, new DateTime(2025, 11, 25, 12, 0, 0, DateTimeKind.Utc), 40m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR47", ex.Id);
    }

    // Se a trilha Penalty do período já existe (relay anterior), o método reusa o par sem recalcular nem duplicar (PenaltyMaterialized = false).
    [Fact]
    public void MaterializeLatePaymentPenalty_WhenPenaltyAlreadyExists_ShouldReuseExistingPair()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);
        var existingPenaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);

        var settlement = contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 10, 28, 12, 0, 0, DateTimeKind.Utc), 1020m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.False(settlement.PenaltyMaterialized);
        Assert.Equal(existingPenaltyOutflow.Id, settlement.PenaltyCommitmentId);
        Assert.Equal(20m, settlement.PenaltyAmountValue);
        Assert.Equal(2, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty));
    }

    // Apontar para o commitment de inflow (ocupação) lança ECC.CTR15 — só a perna de pagamento (outflow) é alvo do one-shot.
    [Fact]
    public void MaterializeLatePaymentPenalty_OnInflowCommitment_ShouldThrowECC_CTR15()
    {
        var (contract, _) = ActivatedOneMonth();
        var rentInflow = contract.FindPromisedCommitment(
            EconomicContractMother.October2025(), CommitmentDirection.InflowPromise, CommitmentPurpose.Rent);

        var ex = Assert.Throws<DomainException>(() => contract.MaterializeLatePaymentPenalty(
            rentInflow.Id, new DateTime(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc), 1020m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR15", ex.Id);
    }

    // Contrato ainda em Draft não aceita pagamento em atraso: lança ECC.CTR05 antes de qualquer lookup.
    [Fact]
    public void MaterializeLatePaymentPenalty_OnDraftContract_ShouldThrowECC_CTR05()
    {
        var contract = EconomicContractMother.New().Build();

        var ex = Assert.Throws<DomainException>(() => contract.MaterializeLatePaymentPenalty(
            EconomicContractMother.OutflowCommitmentIdSlot1, new DateTime(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc), 1020m,
            PenaltyFactory(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR05", ex.Id);
    }

    // A trilha Penalty é auto-liquidante: o evento de caixa que cobre o outflow cumpre as duas pernas; segunda chamada é no-op.
    [Fact]
    public void TrySettlePenaltyCoverage_OnPenaltyLeg_ShouldFulfillBothLegsAndBeIdempotent()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);
        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);

        var settled = contract.TrySettlePenaltyCoverage(penaltyOutflow.Id, CoveringEventId, EconomicContractMother.FixedOccurredAt);
        var secondCall = contract.TrySettlePenaltyCoverage(penaltyOutflow.Id, CoveringEventId, EconomicContractMother.FixedOccurredAt);

        Assert.True(settled);
        Assert.True(secondCall);
        var penaltyCommitments = contract.Commitments.Where(c => c.Purpose == CommitmentPurpose.Penalty).ToList();
        Assert.Equal(2, penaltyCommitments.Count);
        Assert.All(penaltyCommitments, c => Assert.Equal(CommitmentStatus.Fulfilled, c.Status));
    }

    // Para um commitment de trilha não-Penalty o método retorna false sem mutar nada (o chamador segue o fluxo normal de duality).
    [Fact]
    public void TrySettlePenaltyCoverage_OnNonPenaltyLeg_ShouldReturnFalseWithoutMutation()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var settled = contract.TrySettlePenaltyCoverage(rentOutflow, CoveringEventId, EconomicContractMother.FixedOccurredAt);

        Assert.False(settled);
        Assert.Equal(CommitmentStatus.Promised, contract.FindCommitment(rentOutflow).Status);
    }
}
