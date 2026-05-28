namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractActivateTests
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

    // Activate em contrato Draft gera 2×TermMonths commitments Promised e move status para Active.
    [Fact]
    public void Activate_OnDraftContract_ShouldGenerateAllCommitmentsAndTransitionToActive()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(12)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        Assert.Same(ContractStatus.Active, contract.Status);
        Assert.Equal(24, contract.Commitments.Count);
        Assert.Equal(12, contract.Commitments.Count(c => c.Direction == CommitmentDirection.OutflowPromise));
        Assert.Equal(12, contract.Commitments.Count(c => c.Direction == CommitmentDirection.InflowPromise));
        Assert.All(contract.Commitments, c => Assert.Same(CommitmentStatus.Promised, c.Status));
    }

    // Pares mensais gerados por Activate têm ReciprocalLink cruzado outflow ↔ inflow.
    [Fact]
    public void Activate_ShouldLinkReciprocalCommitmentsPerMonth()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(3)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        var outflows = contract.Commitments.Where(c => c.Direction == CommitmentDirection.OutflowPromise).ToList();
        Assert.All(outflows, outflow =>
        {
            Assert.NotNull(outflow.Reciprocal);
            var inflow = contract.Commitments.Single(c => c.Id.Equals(outflow.Reciprocal!.ReciprocalCommitmentId));
            Assert.Same(CommitmentDirection.InflowPromise, inflow.Direction);
            Assert.Equal(outflow.Period, inflow.Period);
            Assert.NotNull(inflow.Reciprocal);
            Assert.Equal(outflow.Id, inflow.Reciprocal!.ReciprocalCommitmentId);
        });
    }

    // Os 12 períodos gerados são sequenciais começando em StartDate.
    [Fact]
    public void Activate_ShouldGenerateSequentialPeriodsFromStartDate()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(12)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        var outflowPeriods = contract.Commitments
            .Where(c => c.Direction == CommitmentDirection.OutflowPromise)
            .OrderBy(c => c.Period.Year).ThenBy(c => c.Period.Month)
            .Select(c => c.Period)
            .ToList();

        for (var n = 0; n < 12; n++)
        {
            var expected = new DateOnly(2025, 10, 1).AddMonths(n);
            Assert.Equal(new CompetencePeriod(expected.Year, expected.Month), outflowPeriods[n]);
        }
    }

    // Activate emite exatamente um ContractActivated, não um por período.
    [Fact]
    public void Activate_ShouldEmitSingleContractActivatedEvent()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(12)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();
        contract.ClearDomainEvents();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        var events = contract.PullDomainEvents().ToList();
        var activated = events.OfType<ContractActivated>().ToList();
        Assert.Single(activated);
        Assert.Equal(contract.Id, activated[0].ContractId);
        Assert.Equal(contract.TenantId, activated[0].TenantId);
        Assert.Equal(12, activated[0].TermMonths);
        Assert.Equal(12, events.OfType<CommitmentsGenerated>().Count());
    }

    // Activate em contrato já Active lança ECC.CTR16.
    [Fact]
    public void Activate_OnAlreadyActiveContract_ShouldThrowECC_CTR16()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();

        var ex = Assert.Throws<DomainException>(
            () => contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory()));

        Assert.Equal("ECC.CTR16", ex.Id);
    }

    // Activate em contrato Terminated lança ECC.CTR16.
    [Fact]
    public void Activate_OnTerminatedContract_ShouldThrowECC_CTR16()
    {
        var contract = EconomicContractMother.New().Build();
        contract.Terminate(EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory()));

        Assert.Equal("ECC.CTR16", ex.Id);
    }

    // Contrato com TermMonths=1 gera exatamente 1 par (2 commitments) na ativação.
    [Fact]
    public void Activate_WithTermMonthsOne_ShouldGenerateSinglePair()
    {
        var contract = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();

        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());

        Assert.Equal(2, contract.Commitments.Count);
        Assert.Same(ContractStatus.Active, contract.Status);
    }
}
