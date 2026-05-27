namespace EconomicCore.UnitTests.Services;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.Services;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.UnitTests.Operational.EconomicEvents.Mothers;

public class DualityMatchingServiceTests
{
    private static readonly DateTime MatchAt = new(2025, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private static EconomicEvent BuildConsumption(CommitmentRef coveringCommitment, Money? amount = null)
        => EconomicEventMother.New()
            .WithId(EconomicEventId.From(new Guid("c0000000-0000-7000-8000-000000000001")))
            .WithDirection(FlowDirection.Outflow)
            .WithAmount(amount ?? EconomicEventMother.DefaultAmount())
            .BuildCoveredWith(coveringCommitment);

    private static EconomicEvent BuildPayment(CommitmentRef coveringCommitment, Money? amount = null)
        => EconomicEventMother.New()
            .WithId(EconomicEventId.From(new Guid("d0000000-0000-7000-8000-000000000002")))
            .WithDirection(FlowDirection.Outflow)
            .WithAmount(amount ?? EconomicEventMother.DefaultAmount())
            .BuildCoveredWith(coveringCommitment);

    // Happy path: payment + consumption cobertos pelo mesmo Commitment → ambos pareiam e emitem DualityClosed.
    [Fact]
    public void Match_WithMatchingPaymentAndConsumption_ShouldCloseDualityOnBothEventsAndEmitEvents()
    {
        var commitmentRef = EconomicEventMother.DefaultCommitment();
        var consumption = BuildConsumption(commitmentRef);
        var payment = BuildPayment(commitmentRef);
        consumption.ClearDomainEvents();
        payment.ClearDomainEvents();

        DualityMatchingService.Match(payment, consumption, MatchAt);

        Assert.NotNull(payment.Duality);
        Assert.NotNull(consumption.Duality);
        Assert.Equal(consumption.Id, payment.Duality!.CounterpartEventId);
        Assert.Equal(payment.Id, consumption.Duality!.CounterpartEventId);
        Assert.Equal(1000m, payment.Duality.MatchedAmount.Amount);
        Assert.Equal(1000m, consumption.Duality.MatchedAmount.Amount);

        var paymentClosed = Assert.IsType<DualityClosed>(Assert.Single(payment.PullDomainEvents()));
        var consumptionClosed = Assert.IsType<DualityClosed>(Assert.Single(consumption.PullDomainEvents()));
        Assert.Equal(consumption.Id, paymentClosed.CounterpartEventId);
        Assert.Equal(payment.Id, consumptionClosed.CounterpartEventId);
    }

    // Amounts diferentes: matching usa o mínimo dos saldos remanescentes (Phase 1 ainda 1:1, mas mecânica funciona).
    [Fact]
    public void Match_WithDifferentAmounts_ShouldMatchMinimumOfBalances()
    {
        var commitmentRef = EconomicEventMother.DefaultCommitment();
        var consumption = BuildConsumption(commitmentRef, new Money(800m, Currency.BRL));
        var payment = BuildPayment(commitmentRef, new Money(1000m, Currency.BRL));

        DualityMatchingService.Match(payment, consumption, MatchAt);

        Assert.Equal(800m, payment.Duality!.MatchedAmount.Amount);
        Assert.Equal(800m, consumption.Duality!.MatchedAmount.Amount);
    }

    // paymentEvent null lança ECC.DMS01.
    [Fact]
    public void Match_WithNullPayment_ShouldThrowECC_DMS01()
    {
        var commitmentRef = EconomicEventMother.DefaultCommitment();
        var consumption = BuildConsumption(commitmentRef);

        var ex = Assert.Throws<DomainException>(
            () => DualityMatchingService.Match(null!, consumption, MatchAt));

        Assert.Equal("ECC.DMS01", ex.Id);
    }

    // consumptionEvent null lança ECC.DMS01.
    [Fact]
    public void Match_WithNullConsumption_ShouldThrowECC_DMS01()
    {
        var commitmentRef = EconomicEventMother.DefaultCommitment();
        var payment = BuildPayment(commitmentRef);

        var ex = Assert.Throws<DomainException>(
            () => DualityMatchingService.Match(payment, null!, MatchAt));

        Assert.Equal("ECC.DMS01", ex.Id);
    }

    // Tenants distintos entre payment e consumption lança ECC.DMS02 (proteção multi-tenant).
    [Fact]
    public void Match_WithDifferentTenants_ShouldThrowECC_DMS02()
    {
        var commitmentRef = EconomicEventMother.DefaultCommitment();
        var consumption = EconomicEventMother.New()
            .WithId(EconomicEventId.From(new Guid("e0000000-0000-7000-8000-000000000003")))
            .BuildCoveredWith(commitmentRef);
        var otherTenantPaymentMother = EconomicEventMother.New()
            .WithId(EconomicEventId.From(new Guid("f0000000-0000-7000-8000-000000000004")));
        // Hack: build payment, then force a different tenant via reflection or a second Mother instance.
        // Mother defaults to FixedTenantId. To diverge, use a different builder altogether: construct directly.
        var differentTenantId = TenantId.From(new Guid("99999999-9999-7999-8999-999999999999"));
        var payment = EconomicEvent.RegisterCovered(
            id: EconomicEventId.From(new Guid("f0000000-0000-7000-8000-000000000004")),
            tenantId: differentTenantId,
            direction: FlowDirection.Outflow,
            resourceId: EconomicEventMother.FixedResourceId,
            amount: EconomicEventMother.DefaultAmount(),
            occurredAt: EconomicEventMother.DefaultOccurredAt(),
            typeId: null,
            participations: EconomicEventMother.DefaultParticipations(),
            coveringCommitment: commitmentRef,
            competence: EconomicEventMother.DefaultCompetence(),
            createdBy: null,
            registeredAt: EconomicEventMother.FixedRegisteredAt);

        var ex = Assert.Throws<DomainException>(
            () => DualityMatchingService.Match(payment, consumption, MatchAt));

        Assert.Equal("ECC.DMS02", ex.Id);
    }

    // Eventos cobertos por commitments recíprocos (IDs diferentes) pareiam normalmente — validação de reciprocidade é do Contract.
    [Fact]
    public void Match_WithDifferentCoveringCommitments_ShouldSucceed()
    {
        var outflowRef = new CommitmentRef(CommitmentId.From(new Guid("aaaa0001-0001-7001-8001-aaaaaaaaaaaa")));
        var inflowRef = new CommitmentRef(CommitmentId.From(new Guid("bbbb0001-0001-7001-8001-bbbbbbbbbbbb")));
        var consumption = BuildConsumption(inflowRef);
        var payment = BuildPayment(outflowRef);
        consumption.ClearDomainEvents();
        payment.ClearDomainEvents();

        DualityMatchingService.Match(payment, consumption, MatchAt);

        Assert.NotNull(payment.Duality);
        Assert.NotNull(consumption.Duality);
    }
}
