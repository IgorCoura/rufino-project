namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractAdjustmentTests
{
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

    private static EconomicContract ActivatedTwelveMonths()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(12)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();
        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());
        return contract;
    }

    // Reajuste a partir de uma competência re-precifica os commitments Promised futuros da trilha; os anteriores ficam.
    [Fact]
    public void ApplyAdjustment_FromCompetence_ShouldRepriceOpenFutureCommitmentsOnly()
    {
        var contract = ActivatedTwelveMonths();

        contract.ApplyAdjustmentToAmount(CommitmentPurpose.Rent, 2026, 4, 1100m, Currency.BRL, EconomicContractMother.FixedOccurredAt);

        var beforeRepriced = contract.Commitments
            .Where(c => c.Purpose == CommitmentPurpose.Rent && c.Period.Year == 2025);
        var afterRepriced = contract.Commitments
            .Where(c => c.Purpose == CommitmentPurpose.Rent
                && (c.Period.Year > 2026 || (c.Period.Year == 2026 && c.Period.Month >= 4)));

        Assert.All(beforeRepriced, c => Assert.Equal(1000m, c.ExpectedAmount.Amount));
        Assert.All(afterRepriced, c => Assert.Equal(1100m, c.ExpectedAmount.Amount));
    }

    // ApplyAdjustmentByRate calcula o novo valor (atual × (1 + índice)) dentro do agregado: 1000 × 1.10 = 1100.
    [Fact]
    public void ApplyAdjustmentByRate_ShouldComputeFromCurrentAmount()
    {
        var contract = ActivatedTwelveMonths();

        var applied = contract.ApplyAdjustmentByRate(CommitmentPurpose.Rent, 2026, 4, 0.10m, EconomicContractMother.FixedOccurredAt);

        Assert.Equal(1100m, applied.Amount);
        var lastMonth = contract.Commitments.Single(c => c.Purpose == CommitmentPurpose.Rent
            && c.Direction == CommitmentDirection.OutflowPromise && c.Period.Year == 2026 && c.Period.Month == 9);
        Assert.Equal(1100m, lastMonth.ExpectedAmount.Amount);
    }

    // Reajuste cuja competência alcança um período já cumprido (travado) lança ECC.CTR40.
    [Fact]
    public void ApplyAdjustment_OverFulfilledPeriod_ShouldThrowECC_CTR40()
    {
        var contract = ActivatedTwelveMonths();
        var octoberOutflow = contract.FindPromisedCommitment(
            EconomicContractMother.October2025(), CommitmentDirection.OutflowPromise, CommitmentPurpose.Rent);
        contract.MarkFulfilled(octoberOutflow.Id, EconomicEventIdStub(), EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(() => contract.ApplyAdjustmentToAmount(
            CommitmentPurpose.Rent, 2025, 10, 1100m, Currency.BRL, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR40", ex.Id);
    }

    // Reajuste sem nenhum commitment em aberto no intervalo lança ECC.CTR41.
    [Fact]
    public void ApplyAdjustment_WithNoOpenCommitmentsInRange_ShouldThrowECC_CTR41()
    {
        var contract = ActivatedTwelveMonths();

        var ex = Assert.Throws<DomainException>(() => contract.ApplyAdjustmentToAmount(
            CommitmentPurpose.Rent, 2030, 1, 1100m, Currency.BRL, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR41", ex.Id);
    }

    private static Domain.Operational.EconomicEvents.EconomicEventId EconomicEventIdStub()
        => Domain.Operational.EconomicEvents.EconomicEventId.From(new Guid("70000000-0000-7000-8000-000000000099"));
}
