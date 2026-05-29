namespace EconomicCore.Infra.Outbox.Handlers;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.Services;

/// <summary>
/// Reacts to <see cref="EconomicEventRegistered"/> (via the Outbox relay) to do the cross-aggregate work the
/// command handler no longer does: find the reciprocal covered event and, once both exist, close the duality
/// (<see cref="DualityMatchingService"/>) and mark both commitments fulfilled. Runs inside the relay's claim
/// transaction — it does NOT call SaveChanges; the <c>OutboxProcessor</c> persists these mutations together with
/// marking the message processed. At-least-once safe: if the duality is already closed it is a no-op.
/// </summary>
internal sealed class CloseDualityOnEconomicEventRegisteredHandler(
    IEconomicEventRepository eventRepo,
    IEconomicContractRepository contractRepo)
    : IDomainEventHandler<EconomicEventRegistered>
{
    public async Task HandleAsync(EconomicEventRegistered domainEvent, CancellationToken cancellationToken = default)
    {
        // Only commitment-covered events participate in the deferred duality close.
        if (domainEvent.CoveringCommitmentId is not { } coveringCommitmentGuid
            || domainEvent.CoveringContractId is not { } coveringContractGuid)
            return;

        var tenantId = domainEvent.TenantId;
        var commitmentId = CommitmentId.From(coveringCommitmentGuid);
        var contractId = EconomicContractId.From(coveringContractGuid);

        var registeredEvent = await eventRepo.GetByIdAsync(domainEvent.EconomicEventId, tenantId, cancellationToken);
        if (registeredEvent is null || registeredEvent.Duality is not null)
            return; // already matched (reprocessed message) or gone — idempotent no-op

        var contract = await contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken);
        if (contract is null)
            return;

        var reciprocalCommitment = contract.FindReciprocalCommitment(commitmentId);
        var reciprocalEvent = await eventRepo.FindCoveredByCommitmentAsync(reciprocalCommitment.Id, tenantId, cancellationToken);
        if (reciprocalEvent is null)
            return; // counterpart not registered yet — the close runs when its own event arrives

        DualityMatchingService.Match(reciprocalEvent, registeredEvent, domainEvent.OccurredAt);
        MarkFulfilledIfPending(contract, commitmentId, registeredEvent.Id, domainEvent.OccurredAt);
        MarkFulfilledIfPending(contract, reciprocalCommitment.Id, reciprocalEvent.Id, domainEvent.OccurredAt);
    }

    private static void MarkFulfilledIfPending(
        EconomicContract contract, CommitmentId commitmentId, EconomicEventId fulfillingEventId, DateTime occurredAt)
    {
        var commitment = contract.FindCommitment(commitmentId);
        if (commitment.Status == CommitmentStatus.Fulfilled)
            return; // idempotent: a reprocessed relay message must not re-fulfill

        contract.MarkFulfilled(commitmentId, fulfillingEventId, occurredAt);
    }
}
