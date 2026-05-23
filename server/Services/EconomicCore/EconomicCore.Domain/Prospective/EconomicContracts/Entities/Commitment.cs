namespace EconomicCore.Domain.Prospective.EconomicContracts.Entities;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class Commitment : Entity<CommitmentId>
{
    public CommitmentDirection Direction { get; private set; } = default!;
    public CompetencePeriod Period { get; private set; } = default!;
    public Money ExpectedAmount { get; private set; } = default!;
    public DateRange FulfillmentWindow { get; private set; } = default!;
    public CommitmentStatus Status { get; private set; } = default!;
    public ReciprocalLink? Reciprocal { get; private set; }
    public EconomicEventId? FulfillingEventId { get; private set; }

    private Commitment() : base() { }

    internal Commitment(
        CommitmentId id,
        CommitmentDirection direction,
        CompetencePeriod period,
        Money expectedAmount,
        DateRange fulfillmentWindow,
        DateTime occurredAt) : base(id)
    {
        Direction = direction;
        Period = period;
        ExpectedAmount = expectedAmount;
        FulfillmentWindow = fulfillmentWindow;
        Status = CommitmentStatus.Promised;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    internal void SetReciprocal(ReciprocalLink reciprocal)
    {
        Reciprocal = reciprocal;
    }

    internal void MarkFulfilledBy(EconomicEventId fulfillingEventId, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(CommitmentStatus.Fulfilled))
            throw EconomicContractErrors.CannotFulfillInStatus(Status.Name);

        FulfillingEventId = fulfillingEventId;
        Status = CommitmentStatus.Fulfilled;
        UpdatedAt = occurredAt;
    }

    internal void MarkExpired(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(CommitmentStatus.Expired))
            throw EconomicContractErrors.CannotFulfillInStatus(Status.Name);

        Status = CommitmentStatus.Expired;
        UpdatedAt = occurredAt;
    }

    internal void MarkCancelled(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(CommitmentStatus.Cancelled))
            throw EconomicContractErrors.CannotFulfillInStatus(Status.Name);

        Status = CommitmentStatus.Cancelled;
        UpdatedAt = occurredAt;
    }
}
