namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractTerminateTests
{
    // Terminate em contrato Draft é permitido (descartar contrato sem ativar) e emite ContractTerminated sem commitments.
    [Fact]
    public void Terminate_OnDraftContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().Build();
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        Assert.Empty(contract.Commitments);

        var terminated = Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(contract.Id, terminated.ContractId);
        Assert.Equal(contract.TenantId, terminated.TenantId);
        Assert.Equal(EconomicContractMother.FixedOccurredAt, terminated.OccurredAt);
    }

    // Terminate em contrato Active move status para Terminated e emite ContractTerminated.
    [Fact]
    public void Terminate_OnActiveContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        var terminated = Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(contract.Id, terminated.ContractId);
    }

    // Terminate em contrato Suspended também é permitido e emite ContractTerminated.
    [Fact]
    public void Terminate_OnSuspendedContract_ShouldTransitionToTerminatedAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Suspend(EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();

        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        Assert.IsType<ContractTerminated>(Assert.Single(contract.PullDomainEvents()));
    }

    // Terminate em contrato já Terminated lança ECC.CTR13 (transição inválida).
    [Fact]
    public void Terminate_OnAlreadyTerminatedContract_ShouldThrowECC_CTR13()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR13", ex.Id);
    }

    // Terminate no meio do termo cancela em cascata os commitments pendentes de períodos futuros (Period > terminationDate), preservando os já iniciados, e emite um CommitmentCancelled por commitment cancelado.
    [Fact]
    public void Terminate_WithPendingFutureCommitments_ShouldCancelFutureOnesInCascade()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.GenerateCommitmentsFor(EconomicContractMother.November2025(),
            EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.InflowCommitmentIdSlot2,
            EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();

        contract.Terminate(new DateOnly(2025, 10, 15), lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Same(ContractStatus.Terminated, contract.Status);
        Assert.All(
            contract.Commitments.Where(c => c.Period.Equals(EconomicContractMother.November2025())),
            c => Assert.Same(CommitmentStatus.Cancelled, c.Status));
        Assert.All(
            contract.Commitments.Where(c => c.Period.Equals(EconomicContractMother.October2025())),
            c => Assert.Same(CommitmentStatus.Promised, c.Status));

        var events = contract.PullDomainEvents();
        Assert.Equal(2, events.OfType<CommitmentCancelled>().Count());
        Assert.Single(events.OfType<ContractTerminated>());
    }

    // Terminate retorna a quantidade de commitments cancelados pela cascata (2 pernas do período futuro).
    [Fact]
    public void Terminate_WithPendingFutureCommitments_ShouldReturnCancelledCount()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.GenerateCommitmentsFor(EconomicContractMother.November2025(),
            EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.InflowCommitmentIdSlot2,
            EconomicContractMother.FixedOccurredAt);

        var cancelledCount = contract.Terminate(new DateOnly(2025, 10, 15), lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Equal(2, cancelledCount);
    }

    // Terminate sem commitments futuros pendentes retorna zero (nada a cancelar na cascata).
    [Fact]
    public void Terminate_WithoutFutureCommitments_ShouldReturnZero()
    {
        var contract = EconomicContractMother.New().Build();

        var cancelledCount = contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        Assert.Equal(0, cancelledCount);
    }

    // Terminate com terminationDate anterior ao último dia do período inflow ocupado lança ECC.CTR20 (não pode encerrar antes do mês já ocupado).
    [Fact]
    public void Terminate_WithDateBeforeLastOccupiedInflowPeriod_ShouldThrowECC_CTR20()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();

        var ex = Assert.Throws<DomainException>(
            () => contract.Terminate(new DateOnly(2025, 11, 15), EconomicContractMother.November2025(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR20", ex.Id);
    }
}
