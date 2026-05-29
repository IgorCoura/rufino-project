namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Operational.EconomicEvents.Mothers;

public class EconomicEventTests
{
    // RegisterCovered inicializa o estado, fixa CoveringCommitment e emite EconomicEventRegistered com payload.
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
        Assert.NotNull(ev.CoveringCommitment);
        Assert.Equal(EconomicEventMother.FixedContractId, ev.CoveringCommitment!.ContractId);
        Assert.Equal(EconomicEventMother.FixedCommitmentId, ev.CoveringCommitment.CommitmentId);
        Assert.Null(ev.Duality);
        Assert.Equal(2, ev.Participations.Count);

        var registered = Assert.IsType<EconomicEventRegistered>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(ev.Id, registered.EconomicEventId);
        Assert.Equal(FlowDirection.Outflow.Name, registered.DirectionName);
        Assert.Equal(EconomicEventMother.FixedContractId.Value, registered.CoveringContractId);
        Assert.Equal(EconomicEventMother.FixedCommitmentId.Value, registered.CoveringCommitmentId);
        Assert.Null(registered.CounterpartEventId);
        Assert.Equal(1000m, registered.AmountValue);
        Assert.Equal(Currency.BRL.Name, registered.AmountCurrency);
        Assert.Equal(2025, registered.CompetenceYear);
        Assert.Equal(9, registered.CompetenceMonth);
    }

    // RegisterPaired inicializa o estado com Duality preenchida e emite evento com CounterpartEventId no payload.
    [Fact]
    public void RegisterPaired_WithValidInputs_ShouldInitializeStateAndEmitEvent()
    {
        var ev = EconomicEventMother.New().BuildPaired();

        Assert.NotNull(ev.Duality);
        Assert.Equal(EconomicEventMother.CounterpartEventId, ev.Duality!.CounterpartEventId);
        Assert.Null(ev.CoveringCommitment);

        var registered = (EconomicEventRegistered)ev.PullDomainEvents()[0];
        Assert.Equal(EconomicEventMother.CounterpartEventId.Value, registered.CounterpartEventId);
        Assert.Null(registered.CoveringContractId);
        Assert.Null(registered.CoveringCommitmentId);
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

    // CloseDuality total move o evento para totalmente pareado e emite DualityClosed.
    [Fact]
    public void CloseDuality_WithFullMatch_ShouldFullyPairEventAndEmitDualityClosed()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        ev.ClearDomainEvents();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        ev.CloseDuality(counterpartId, EconomicEventMother.DefaultAmount(), EconomicEventMother.FixedRegisteredAt.AddMinutes(5));

        Assert.NotNull(ev.Duality);
        Assert.Equal(counterpartId, ev.Duality!.CounterpartEventId);
        Assert.Equal(1000m, ev.Duality.MatchedAmount.Amount);

        var closed = Assert.IsType<DualityClosed>(Assert.Single(ev.PullDomainEvents()));
        Assert.Equal(ev.Id, closed.EconomicEventId);
        Assert.Equal(counterpartId, closed.CounterpartEventId);
        Assert.Equal(1000m, closed.MatchedAmountValue);
    }

    // CloseDuality parcial acumula MatchedAmount no DualityLink; segundo CloseDuality completa o pareamento.
    [Fact]
    public void CloseDuality_PartialThenFull_ShouldAccumulateMatchedAmount()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        ev.ClearDomainEvents();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        ev.CloseDuality(counterpartId, new Money(400m, Currency.BRL), EconomicEventMother.FixedRegisteredAt);
        Assert.Equal(400m, ev.Duality!.MatchedAmount.Amount);

        ev.CloseDuality(counterpartId, new Money(600m, Currency.BRL), EconomicEventMother.FixedRegisteredAt.AddSeconds(1));
        Assert.Equal(1000m, ev.Duality.MatchedAmount.Amount);

        Assert.Equal(2, ev.DomainEvents.Count);
    }

    // CloseDuality em evento já totalmente pareado lança ECC.EVT05.
    [Fact]
    public void CloseDuality_OnFullyMatchedEvent_ShouldThrowECC_EVT05()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));
        ev.CloseDuality(counterpartId, EconomicEventMother.DefaultAmount(), EconomicEventMother.FixedRegisteredAt);

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(counterpartId, new Money(1m, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT05", ex.Id);
    }

    // CloseDuality com MatchedAmount maior que saldo restante lança ECC.EVT06.
    [Fact]
    public void CloseDuality_ExceedingRemainingBalance_ShouldThrowECC_EVT06()
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(counterpartId, new Money(1500m, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT06", ex.Id);
    }

    // CloseDuality com MatchedAmount zero ou negativo lança ECC.EVT06 (não pode reduzir o saldo).
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CloseDuality_WithNonPositiveAmount_ShouldThrowECC_EVT06(double amount)
    {
        var ev = EconomicEventMother.New().BuildCovered();
        var counterpartId = EconomicEventId.From(new Guid("99999999-9999-7999-8999-999999999999"));

        var ex = Assert.Throws<DomainException>(
            () => ev.CloseDuality(counterpartId, new Money((decimal)amount, Currency.BRL), EconomicEventMother.FixedRegisteredAt));

        Assert.Equal("ECC.EVT06", ex.Id);
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
}
