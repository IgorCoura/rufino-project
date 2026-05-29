namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractTests
{
    // Create válido inicia em Draft, sem commitments, e emite EconomicContractCreated com os novos campos.
    [Fact]
    public void Create_WithValidInputs_ShouldStartDraftAndEmitCreatedEvent()
    {
        var contract = EconomicContractMother.New().Build();

        Assert.Equal(EconomicContractMother.FixedContractId, contract.Id);
        Assert.Same(ContractStatus.Draft, contract.Status);
        Assert.Empty(contract.Commitments);
        Assert.Equal(EconomicContractMother.FixedResourceId, contract.ResourceId);
        Assert.Equal(EconomicContractMother.DEFAULT_TERM_MONTHS, contract.TermMonths);
        Assert.Equal(EconomicContractMother.FixedStartDate, contract.StartDate);

        var created = Assert.IsType<EconomicContractCreated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(contract.Id, created.ContractId);
        Assert.Equal(contract.ResourceId, created.ResourceId);
        Assert.Equal(contract.TermMonths, created.TermMonths);
        Assert.Equal(contract.StartDate, created.StartDate);
        Assert.Equal(ContractDirection.Acquisition.Name, created.DirectionName);
        Assert.Equal(Periodicity.Monthly.Name, created.PeriodicityName);
        Assert.Equal(5, created.AnchorDay);
        Assert.Equal(1000m, created.ExpectedAmountValue);
    }

    // TermMonths fora da faixa 1-120 lança ECC.CTR17.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Create_WithInvalidTermMonths_ShouldThrowECC_CTR17(int termMonths)
    {
        var ex = Assert.Throws<DomainException>(
            () => EconomicContractMother.New().WithTermMonths(termMonths).Build());

        Assert.Equal("ECC.CTR17", ex.Id);
    }

    // StartDate mais de 1 ano no passado em relação a occurredAt lança ECC.CTR18.
    [Fact]
    public void Create_WithStartDateTooFarInPast_ShouldThrowECC_CTR18()
    {
        var tooOld = DateOnly.FromDateTime(EconomicContractMother.FixedOccurredAt).AddYears(-1).AddDays(-1);

        var ex = Assert.Throws<DomainException>(
            () => EconomicContractMother.New().WithStartDate(tooOld).Build());

        Assert.Equal("ECC.CTR18", ex.Id);
    }

    // GenerateCommitmentsFor cria par outflow+inflow com ReciprocalLink cruzado e emite CommitmentsGenerated.
    [Fact]
    public void GenerateCommitmentsFor_WithActiveContract_ShouldCreatePairAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.ClearDomainEvents();

        contract.GenerateCommitmentsFor(
            EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1,
            EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);

        Assert.Equal(2, contract.Commitments.Count);
        var outflow = contract.Commitments.Single(c => c.Direction == CommitmentDirection.OutflowPromise);
        var inflow = contract.Commitments.Single(c => c.Direction == CommitmentDirection.InflowPromise);
        Assert.Same(CommitmentStatus.Promised, outflow.Status);
        Assert.Same(CommitmentStatus.Promised, inflow.Status);
        Assert.NotNull(outflow.Reciprocal);
        Assert.Equal(inflow.Id, outflow.Reciprocal!.ReciprocalCommitmentId);
        Assert.NotNull(inflow.Reciprocal);
        Assert.Equal(outflow.Id, inflow.Reciprocal!.ReciprocalCommitmentId);

        var generated = Assert.IsType<CommitmentsGenerated>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(2025, generated.PeriodYear);
        Assert.Equal(10, generated.PeriodMonth);
        Assert.Equal(outflow.Id, generated.OutflowCommitmentId);
        Assert.Equal(inflow.Id, generated.InflowCommitmentId);
    }

    // Gerar segunda vez no mesmo período viola idempotência ECC.CTR02.
    [Fact]
    public void GenerateCommitmentsFor_DuplicatePeriod_ShouldThrowECC_CTR02()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(() => contract.GenerateCommitmentsFor(
            EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot2, EconomicContractMother.InflowCommitmentIdSlot2,
            EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR02", ex.Id);
    }

    // Gerar com contrato Terminated lança ECC.CTR05.
    [Fact]
    public void GenerateCommitmentsFor_OnTerminatedContract_ShouldThrowECC_CTR05()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(() => contract.GenerateCommitmentsFor(
            EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR05", ex.Id);
    }

    // Gerar com contrato Suspended lança ECC.CTR05.
    [Fact]
    public void GenerateCommitmentsFor_OnSuspendedContract_ShouldThrowECC_CTR05()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Suspend(EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(() => contract.GenerateCommitmentsFor(
            EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR05", ex.Id);
    }

    // MarkFulfilled de commitment Promised muda Status para Fulfilled e emite CommitmentFulfilled.
    [Fact]
    public void MarkFulfilled_OnPromisedCommitment_ShouldTransitionAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();
        var fulfillingEventId = EconomicEventId.From(new Guid("66666666-6666-7666-8666-666666666666"));

        contract.MarkFulfilled(EconomicContractMother.OutflowCommitmentIdSlot1, fulfillingEventId, EconomicContractMother.FixedOccurredAt);

        var commitment = contract.Commitments.Single(c => c.Id.Equals(EconomicContractMother.OutflowCommitmentIdSlot1));
        Assert.Same(CommitmentStatus.Fulfilled, commitment.Status);
        Assert.Equal(fulfillingEventId, commitment.FulfillingEventId);

        var fulfilled = Assert.IsType<CommitmentFulfilled>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(EconomicContractMother.OutflowCommitmentIdSlot1, fulfilled.CommitmentId);
        Assert.Equal(fulfillingEventId, fulfilled.FulfillingEventId);
    }

    // MarkFulfilled em commitment já Fulfilled/Cancelled/Expired lança ECC.CTR03.
    [Fact]
    public void MarkFulfilled_OnTerminalCommitment_ShouldThrowECC_CTR03()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        var fulfillingEventId = EconomicEventId.From(new Guid("66666666-6666-7666-8666-666666666666"));
        contract.MarkFulfilled(EconomicContractMother.OutflowCommitmentIdSlot1, fulfillingEventId, EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => contract.MarkFulfilled(EconomicContractMother.OutflowCommitmentIdSlot1, fulfillingEventId, EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR03", ex.Id);
    }

    // Expire move commitment Promised para Expired e emite CommitmentExpired.
    [Fact]
    public void Expire_OnPromisedCommitment_ShouldTransitionAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();

        contract.Expire(EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.FixedOccurredAt);

        var commitment = contract.Commitments.Single(c => c.Id.Equals(EconomicContractMother.OutflowCommitmentIdSlot1));
        Assert.Same(CommitmentStatus.Expired, commitment.Status);

        var expired = Assert.IsType<CommitmentExpired>(Assert.Single(contract.PullDomainEvents()));
        Assert.Equal(EconomicContractMother.OutflowCommitmentIdSlot1, expired.CommitmentId);
    }

    // CancelCommitment move commitment Promised para Cancelled e emite CommitmentCancelled.
    [Fact]
    public void CancelCommitment_OnPromisedCommitment_ShouldTransitionAndEmitEvent()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.GenerateCommitmentsFor(EconomicContractMother.October2025(),
            EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.InflowCommitmentIdSlot1,
            EconomicContractMother.FixedOccurredAt);
        contract.ClearDomainEvents();

        contract.CancelCommitment(EconomicContractMother.OutflowCommitmentIdSlot1, EconomicContractMother.FixedOccurredAt);

        var commitment = contract.Commitments.Single(c => c.Id.Equals(EconomicContractMother.OutflowCommitmentIdSlot1));
        Assert.Same(CommitmentStatus.Cancelled, commitment.Status);
        Assert.IsType<CommitmentCancelled>(Assert.Single(contract.PullDomainEvents()));
    }

    // Operações sobre CommitmentId inexistente lançam ECC.CTR12.
    [Fact]
    public void MarkFulfilled_WithUnknownCommitmentId_ShouldThrowECC_CTR12()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        var unknown = CommitmentId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = Assert.Throws<DomainException>(
            () => contract.MarkFulfilled(unknown, EconomicEventId.New(), EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR12", ex.Id);
    }

    // Suspend/Resume/Terminate seguem a máquina de estados de ContractStatus.
    [Fact]
    public void Lifecycle_SuspendResumeTerminate_ShouldFollowStateMachine()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();

        contract.Suspend(EconomicContractMother.FixedOccurredAt);
        Assert.Same(ContractStatus.Suspended, contract.Status);

        contract.Resume(EconomicContractMother.FixedOccurredAt);
        Assert.Same(ContractStatus.Active, contract.Status);

        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);
        Assert.Same(ContractStatus.Terminated, contract.Status);
    }

    // Transições inválidas (Terminated → qualquer coisa) lançam ECC.CTR13.
    [Fact]
    public void Lifecycle_FromTerminated_ShouldThrowECC_CTR13()
    {
        var contract = EconomicContractMother.New().BuildActiveEmpty();
        contract.Terminate(EconomicContractMother.FixedStartDate, lastOccupiedInflowPeriod: null, EconomicContractMother.FixedOccurredAt);

        var ex = Assert.Throws<DomainException>(() => contract.Suspend(EconomicContractMother.FixedOccurredAt));

        Assert.Equal("ECC.CTR13", ex.Id);
    }
}
