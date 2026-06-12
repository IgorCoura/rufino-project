namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Operational.EconomicEvents.Mothers;

public class EconomicEventTests
{
    // RegisterCovered inicializa o estado, gera uma única alocação de cobertura e emite EconomicEventRegistered.
    [Fact]
    public void RegisterCovered_WithValidInputs_ShouldInitializeStateAndEmitEvent()
    {
        var ev = EconomicEventMother.New().BuildCovered();

        Assert.Equal(EconomicEventMother.FixedEventId, ev.Id);
        Assert.Equal(EconomicEventMother.FixedTenantId, ev.TenantId);
        Assert.Same(FlowDirection.Outflow, ev.Direction);
        Assert.Equal(EconomicEventMother.FixedResourceId, ev.ResourceId);
        Assert.Equal(1000m, ev.Amount.Amount);
        Assert.Equal(EconomicEventMother.FixedOccurredAtUtc, ev.OccurredAt.InstantUtc);
        var allocation = Assert.Single(ev.Allocations);
        Assert.Equal(EconomicEventMother.FixedContractId, allocation.Commitment.ContractId);
        Assert.Equal(EconomicEventMother.FixedCommitmentId, allocation.Commitment.CommitmentId);
        Assert.Equal(1000m, allocation.Amount.Amount);
        Assert.Empty(ev.DualityLinks);
        Assert.Equal(2, ev.Participations.Count);

        var registered = Assert.IsType<EconomicEventRegistered>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(ev.Id, registered.EconomicEventId);
        Assert.Equal(FlowDirection.Outflow.Name, registered.DirectionName);
        var covering = Assert.Single(registered.Coverings);
        Assert.Equal(EconomicEventMother.FixedContractId.Value, covering.ContractId);
        Assert.Equal(EconomicEventMother.FixedCommitmentId.Value, covering.CommitmentId);
        Assert.Null(registered.CounterpartEventId);
        Assert.Equal(1000m, registered.AmountValue);
        Assert.Equal(Currency.BRL.Name, registered.AmountCurrency);
        Assert.Equal(2025, registered.CompetenceYear);
        Assert.Equal(9, registered.CompetenceMonth);
    }

    // RegisterPaired inicializa com um DualityLink direto (sem alocação de cobertura) e emite evento com CounterpartEventId.
    [Fact]
    public void RegisterPaired_WithValidInputs_ShouldInitializeStateAndEmitEvent()
    {
        var ev = EconomicEventMother.New().BuildPaired();

        var link = Assert.Single(ev.DualityLinks);
        Assert.Equal(EconomicEventMother.CounterpartEventId, link.CounterpartEventId);
        Assert.Null(link.CommitmentId);
        Assert.Empty(ev.Allocations);

        var registered = (EconomicEventRegistered)ev.PullDomainEvents()[0];
        Assert.Equal(EconomicEventMother.CounterpartEventId.Value, registered.CounterpartEventId);
        Assert.Empty(registered.Coverings);
    }

    // RegisterCovered com CommitmentRef null lança ECC.EVT04 (anti-orfandade — INV crítica).
    [Fact]
    public void RegisterCovered_WithNullCommitment_ShouldThrowECC_EVT04()
    {
        var mother = EconomicEventMother.New();

        var ex = Assert.Throws<DomainException>(() => mother.BuildCoveredWith(null!));

        Assert.Equal("ECC.EVT04", ex.Id);
    }

    // RegisterPaired com DualityLink null lança ECC.EVT04 (anti-orfandade — INV crítica).
    [Fact]
    public void RegisterPaired_WithNullDuality_ShouldThrowECC_EVT04()
    {
        var mother = EconomicEventMother.New();

        var ex = Assert.Throws<DomainException>(() => mother.BuildPairedWith(null!));

        Assert.Equal("ECC.EVT04", ex.Id);
    }

    // Participations sem ao menos um Provider e um Recipient distintos lança ECC.EVT01 (Axiom 3).
    [Fact]
    public void Register_WithoutProvider_ShouldThrowECC_EVT01()
    {
        var onlyRecipient = new List<Participation>
        {
            new(EconomicEventMother.RecipientAgentId, ParticipationRole.Recipient),
        };

        var ex = Assert.Throws<DomainException>(
            () => EconomicEventMother.New().WithParticipations(onlyRecipient).BuildCovered());

        Assert.Equal("ECC.EVT01", ex.Id);
    }

    // Participations sem ao menos um Recipient lança ECC.EVT01.
    [Fact]
    public void Register_WithoutRecipient_ShouldThrowECC_EVT01()
    {
        var onlyProvider = new List<Participation>
        {
            new(EconomicEventMother.ProviderAgentId, ParticipationRole.Provider),
        };

        var ex = Assert.Throws<DomainException>(
            () => EconomicEventMother.New().WithParticipations(onlyProvider).BuildCovered());

        Assert.Equal("ECC.EVT01", ex.Id);
    }

    // Amount zero ou negativo lança ECC.EVT02.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Register_WithNonPositiveAmount_ShouldThrowECC_EVT02(double amount)
    {
        var ex = Assert.Throws<DomainException>(
            () => EconomicEventMother.New().WithAmount(new Money((decimal)amount, Currency.BRL)).BuildCovered());

        Assert.Equal("ECC.EVT02", ex.Id);
    }

    // ResourceId vazio (Empty) lança ECC.EVT03 (Axiom 1).
    [Fact]
    public void Register_WithEmptyResourceId_ShouldThrowECC_EVT03()
    {
        var ex = Assert.Throws<DomainException>(
            () => EconomicEventMother.New().WithResourceId(EconomicResourceId.Empty).BuildCovered());

        Assert.Equal("ECC.EVT03", ex.Id);
    }

    // CloseDuality total da alocação marca o evento como totalmente pareado e emite DualityClosed.
    [Fact]
    public void CloseDuality_WithFullMatch_ShouldFullyPairEventAndEmitDualityClosed()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        ev.ClearDomainEvents();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        ev.CloseDuality(commitmentId, counterpartId, EconomicEventMother.DefaultAmount(), EconomicEventMother.FixedRegisteredAt.AddMinutes(5));

        var link = Assert.Single(ev.DualityLinks);
        Assert.Equal(counterpartId, link.CounterpartEventId);
        Assert.Equal(commitmentId, link.CommitmentId);
        Assert.Equal(1000m, ev.MatchedAmountFor(commitmentId));
        Assert.True(ev.IsFullyMatched);

        var closed = Assert.IsType<DualityClosed>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(ev.Id, closed.EconomicEventId);
        Assert.Equal(counterpartId, closed.CounterpartEventId);
        Assert.Equal(1000m, closed.MatchedAmountValue);
    }

    // Dois CloseDuality parciais para a mesma alocação acumulam MatchedAmount (um DualityLink por leg).
    [Fact]
    public void CloseDuality_PartialThenFull_ShouldAccumulateMatchedAmount()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        ev.ClearDomainEvents();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        ev.CloseDuality(commitmentId, counterpartId, new Money(400m, Currency.BRL), EconomicEventMother.FixedRegisteredAt);
        Assert.Equal(400m, ev.MatchedAmountFor(commitmentId));

        ev.CloseDuality(commitmentId, counterpartId, new Money(600m, Currency.BRL), EconomicEventMother.FixedRegisteredAt.AddSeconds(1));
        Assert.Equal(1000m, ev.MatchedAmountFor(commitmentId));

        Assert.Equal(2, ev.DualityLinks.Count);
        Assert.Equal(2, ev.DomainEvents.Count);
    }

    // CloseDuality numa alocação já totalmente pareada lança ECC.EVT05.
    [Fact]
    public void CloseDuality_OnFullyMatchedEvent_ShouldThrowECC_EVT05()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));
        ev.CloseDuality(commitmentId, counterpartId, EconomicEventMother.DefaultAmount(), EconomicEventMother.FixedRegisteredAt);

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(commitmentId, counterpartId, new Money(1m, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT05", ex.Id);
    }

    // CloseDuality para um commitment sem alocação correspondente lança ECC.EVT18.
    [Fact]
    public void CloseDuality_ForUnknownCommitment_ShouldThrowECC_EVT18()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));
        var unknownCommitment = CommitmentId.From(new Guid("00000000-0000-7000-8000-00000000aaaa"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(unknownCommitment, counterpartId, EconomicEventMother.DefaultAmount(), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT18", ex.Id);
    }

    // CloseDuality com MatchedAmount maior que saldo restante da alocação lança ECC.EVT06.
    [Fact]
    public void CloseDuality_ExceedingRemainingBalance_ShouldThrowECC_EVT06()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(commitmentId, counterpartId, new Money(1500m, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT06", ex.Id);
    }

    // CloseDuality com MatchedAmount zero ou negativo lança ECC.EVT06 (não pode reduzir o saldo).
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CloseDuality_WithNonPositiveAmount_ShouldThrowECC_EVT06(double amount)
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(commitmentId, counterpartId, new Money((decimal)amount, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT06", ex.Id);
    }

    // CloseDualityAsSelfSettled fecha a alocação contra o próprio evento (counterpart = ele mesmo) pelo saldo restante.
    [Fact]
    public void CloseDualityAsSelfSettled_OnCoveredAllocation_ShouldCloseAgainstItself()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        ev.ClearDomainEvents();
        var commitmentId = EconomicEventMother.FixedCommitmentId;

        ev.CloseDualityAsSelfSettled(commitmentId, EconomicEventMother.FixedRegisteredAt.AddMinutes(5));

        var link = Assert.Single(ev.DualityLinks);
        Assert.Equal(ev.Id, link.CounterpartEventId);
        Assert.Equal(commitmentId, link.CommitmentId);
        Assert.Equal(1000m, ev.MatchedAmountFor(commitmentId));
        Assert.True(ev.IsFullyMatched);

        var closed = Assert.IsType<DualityClosed>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(ev.Id, closed.CounterpartEventId);
        Assert.Equal(1000m, closed.MatchedAmountValue);
    }

    // CloseDualityAsSelfSettled numa perna já totalmente casada é no-op (reprocesso at-least-once do relay).
    [Fact]
    public void CloseDualityAsSelfSettled_WhenAlreadyMatched_ShouldBeNoOp()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var commitmentId = EconomicEventMother.FixedCommitmentId;
        ev.CloseDualityAsSelfSettled(commitmentId, EconomicEventMother.FixedRegisteredAt);

        ev.CloseDualityAsSelfSettled(commitmentId, EconomicEventMother.FixedRegisteredAt.AddMinutes(1));

        Assert.Single(ev.DualityLinks);
        Assert.Equal(1000m, ev.MatchedAmountFor(commitmentId));
    }

    // CloseDualityAsSelfSettled para um commitment sem alocação correspondente lança ECC.EVT18.
    [Fact]
    public void CloseDualityAsSelfSettled_ForUnknownCommitment_ShouldThrowECC_EVT18()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var unknownCommitment = CommitmentId.From(new Guid("00000000-0000-7000-8000-00000000bbbb"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDualityAsSelfSettled(unknownCommitment, EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT18", ex.Id);
    }

    // PullDomainEvents drena a lista de eventos acumulados.
    [Fact]
    public void PullDomainEvents_ShouldDrainEventList()
    {
        var ev = EconomicEventMother.New().BuildCovered();

        var first = ev.PullDomainEvents();
        var second = ev.PullDomainEvents();

        Assert.Single(first);
        Assert.Empty(second);
    }

    private static readonly CommitmentId RentCommitmentId = CommitmentId.From(new Guid("aaaa0001-0001-7001-8001-aaaaaaaaaaaa"));
    private static readonly CommitmentId CondoCommitmentId = CommitmentId.From(new Guid("aaaa0002-0002-7002-8002-aaaaaaaaaaaa"));

    private static EconomicEvent BuildBundled(params BundledPaymentLine[] lines)
        => EconomicEvent.RegisterBundledPayment(
            EconomicEventMother.FixedEventId,
            EconomicEventMother.FixedTenantId,
            EconomicEventMother.FixedResourceId,
            Currency.BRL,
            EconomicEventMother.FixedOccurredAtUtc,
            EconomicEventMother.ProviderAgentId,
            EconomicEventMother.RecipientAgentId,
            lines,
            competenceYear: 2025,
            competenceMonth: 10,
            createdBy: null,
            registeredAt: EconomicEventMother.FixedRegisteredAt);

    // RegisterBundledPayment soma as linhas no Amount total e cria uma alocação por commitment (boleto único).
    [Fact]
    public void RegisterBundledPayment_WithMultipleLines_ShouldSumAmountAndCreateAllocationPerLine()
    {
        var ev = BuildBundled(
            new BundledPaymentLine(EconomicEventMother.FixedContractId.Value, RentCommitmentId.Value, 1000m),
            new BundledPaymentLine(EconomicEventMother.FixedContractId.Value, CondoCommitmentId.Value, 300m));

        Assert.Equal(1300m, ev.Amount.Amount);
        Assert.Equal(2, ev.Allocations.Count);
        Assert.Equal(1000m, ev.Allocations.Single(a => a.Commitment.CommitmentId.Equals(RentCommitmentId)).Amount.Amount);
        Assert.Equal(300m, ev.Allocations.Single(a => a.Commitment.CommitmentId.Equals(CondoCommitmentId)).Amount.Amount);

        var registered = Assert.IsType<EconomicEventRegistered>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(2, registered.Coverings.Count);
        Assert.Equal(1300m, registered.AmountValue);
    }

    // RegisterBundledPayment sem linhas lança ECC.EVT17 (pagamento bundled exige ao menos uma alocação).
    [Fact]
    public void RegisterBundledPayment_WithNoLines_ShouldThrowECC_EVT17()
    {
        var ex = Assert.Throws<DomainException>(() => BuildBundled());

        Assert.Equal("ECC.EVT17", ex.Id);
    }

    // RegisterBundledPayment com data de pagamento futura lança ECC.EVT15.
    [Fact]
    public void RegisterBundledPayment_WithFuturePaidDate_ShouldThrowECC_EVT15()
    {
        var ex = Assert.Throws<DomainException>(() => EconomicEvent.RegisterBundledPayment(
            EconomicEventMother.FixedEventId,
            EconomicEventMother.FixedTenantId,
            EconomicEventMother.FixedResourceId,
            Currency.BRL,
            EconomicEventMother.FixedRegisteredAt.AddDays(1),
            EconomicEventMother.ProviderAgentId,
            EconomicEventMother.RecipientAgentId,
            [new BundledPaymentLine(EconomicEventMother.FixedContractId.Value, RentCommitmentId.Value, 1000m)],
            competenceYear: 2025,
            competenceMonth: 10,
            createdBy: null,
            registeredAt: EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT15", ex.Id);
    }

    // Num pagamento bundled, fechar a duality de uma alocação não marca o evento como totalmente pareado até a outra fechar.
    [Fact]
    public void CloseDuality_OnBundledPayment_ShouldCloseLegsIndependently()
    {
        var ev = BuildBundled(
            new BundledPaymentLine(EconomicEventMother.FixedContractId.Value, RentCommitmentId.Value, 1000m),
            new BundledPaymentLine(EconomicEventMother.FixedContractId.Value, CondoCommitmentId.Value, 300m));
        var rentCounterpart = EconomicEventId.From(new Guid("11110000-0000-7000-8000-000000000001"));
        var condoCounterpart = EconomicEventId.From(new Guid("22220000-0000-7000-8000-000000000002"));

        ev.CloseDuality(RentCommitmentId, rentCounterpart, new Money(1000m, Currency.BRL), EconomicEventMother.FixedRegisteredAt);
        Assert.False(ev.IsFullyMatched);

        ev.CloseDuality(CondoCommitmentId, condoCounterpart, new Money(300m, Currency.BRL), EconomicEventMother.FixedRegisteredAt);
        Assert.True(ev.IsFullyMatched);
    }
}
