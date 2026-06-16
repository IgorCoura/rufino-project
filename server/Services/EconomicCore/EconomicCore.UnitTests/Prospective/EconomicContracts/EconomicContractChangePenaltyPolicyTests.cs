namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractChangePenaltyPolicyTests
{
    private static Func<CommitmentId> SequentialFactory(int seed = 0)
    {
        var counter = seed;
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

    // Alterar a política em Draft substitui o VO e emite ContractPenaltyTermsChanged com o payload da nova política.
    [Fact]
    public void ChangePenaltyPolicy_OnDraftContract_ShouldReplacePolicyAndEmitEvent()
    {
        var contract = EconomicContractMother.New().Build();

        contract.ChangePenaltyPolicy(
            PenaltyValueKind.FixedAmount, 50m,
            PenaltyValueKind.FixedAmount, 10m,
            InterestAccrualPeriod.Daily, EconomicContractMother.FixedOccurredAt);

        Assert.Equal(PenaltyValueKind.FixedAmount, contract.PenaltyPolicy.FineKind);
        Assert.Equal(50m, contract.PenaltyPolicy.FineValue);
        Assert.Equal(PenaltyValueKind.FixedAmount, contract.PenaltyPolicy.InterestKind);
        Assert.Equal(10m, contract.PenaltyPolicy.InterestValue);
        Assert.Equal(InterestAccrualPeriod.Daily, contract.PenaltyPolicy.InterestPeriod);

        var evt = Assert.Single(contract.PullDomainEvents().OfType<ContractPenaltyTermsChanged>());
        Assert.Equal(contract.Id, evt.ContractId);
        Assert.Equal(contract.TenantId, evt.TenantId);
        Assert.Equal("FIXED", evt.FineKindName);
        Assert.Equal(50m, evt.FineValue);
        Assert.Equal("FIXED", evt.InterestKindName);
        Assert.Equal(10m, evt.InterestValue);
        Assert.Equal("DAILY", evt.InterestPeriodName);
        Assert.Equal(EconomicContractMother.FixedOccurredAt, evt.OccurredAt);
    }

    // Alterar a política em Active vale para a próxima penalidade: multa 5% materializa 1000 × 5% = 50 (não mais 20).
    [Fact]
    public void ChangePenaltyPolicy_OnActiveContract_ShouldApplyToNextPenalty()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        contract.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 0.05m,
            PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt);
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), SequentialFactory(1000), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(50m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Penalidade já materializada antes da alteração mantém o valor antigo — a obrigação materializada prevalece.
    [Fact]
    public void ChangePenaltyPolicy_AfterPenaltyMaterialized_ShouldNotRepriceExistingPenalty()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), SequentialFactory(1000), EconomicContractMother.FixedOccurredAt);

        contract.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 0.10m,
            PenaltyValueKind.Percent, 0.05m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt);
        var secondAttempt = contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), SequentialFactory(2000), EconomicContractMother.FixedOccurredAt);

        Assert.False(secondAttempt);
        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(20m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Pagamento one-shot após a alteração reusa a Penalty já materializada sem recalcular (total = base + valor antigo).
    [Fact]
    public void ChangePenaltyPolicy_AfterPenaltyMaterialized_OneShotShouldReuseOldAmount()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), SequentialFactory(1000), EconomicContractMother.FixedOccurredAt);

        contract.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 0.10m,
            PenaltyValueKind.Percent, 0.05m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt);
        var settlement = contract.MaterializeLatePaymentPenalty(
            rentOutflow, new DateTime(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc),
            totalPaidValue: 1020m, SequentialFactory(2000), EconomicContractMother.FixedOccurredAt);

        Assert.False(settlement.PenaltyMaterialized);
        Assert.Equal(20m, settlement.PenaltyAmountValue);
    }

    // Alterar política com contrato Suspended ou Terminated lança ECC.CTR51.
    [Fact]
    public void ChangePenaltyPolicy_OnSuspendedOrTerminatedContract_ShouldThrowECC_CTR51()
    {
        var suspended = EconomicContractMother.New().BuildActiveEmpty();
        suspended.Suspend(EconomicContractMother.FixedOccurredAt);
        var terminated = EconomicContractMother.New().Build();
        terminated.Terminate(new DateOnly(2025, 10, 15), null, EconomicContractMother.FixedOccurredAt);

        var exSuspended = Assert.Throws<DomainException>(() => suspended.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 0.05m, PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt));
        var exTerminated = Assert.Throws<DomainException>(() => terminated.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 0.05m, PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR51", exSuspended.Id);
        Assert.Equal("ECC.CTR51", exTerminated.Id);
    }

    // Política inválida na alteração propaga a validação do VO (percent fora de range → CTR30; fixo negativo → CTR48) sem mutar o contrato.
    [Fact]
    public void ChangePenaltyPolicy_WithInvalidComponents_ShouldPropagateVOErrors()
    {
        var contract = EconomicContractMother.New().Build();
        var originalPolicy = contract.PenaltyPolicy;

        var exPercent = Assert.Throws<DomainException>(() => contract.ChangePenaltyPolicy(
            PenaltyValueKind.Percent, 1.5m, PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt));
        var exFixed = Assert.Throws<DomainException>(() => contract.ChangePenaltyPolicy(
            PenaltyValueKind.FixedAmount, -10m, PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR30", exPercent.Id);
        Assert.Equal("ECC.CTR48", exFixed.Id);
        Assert.Equal(originalPolicy, contract.PenaltyPolicy);
    }
}
