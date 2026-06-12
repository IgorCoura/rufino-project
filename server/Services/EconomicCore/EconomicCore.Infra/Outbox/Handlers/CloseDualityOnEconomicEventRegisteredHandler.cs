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
        // Only commitment-covered events participate in the deferred duality close (a bundled payment carries one
        // covering per allocation; a directly-paired event carries none).
        if (domainEvent.Coverings.Count == 0)
            return;

        var tenantId = domainEvent.TenantId;
        var registeredEvent = await eventRepo.GetByIdAsync(domainEvent.EconomicEventId, tenantId, cancellationToken);
        if (registeredEvent is null)
            return; // gone — idempotent no-op

        foreach (var covering in domainEvent.Coverings)
        {
            var commitmentId = CommitmentId.From(covering.CommitmentId);
            var contractId = EconomicContractId.From(covering.ContractId);

            // Idempotency: this leg was already closed by a prior (reprocessed) message.
            if (registeredEvent.HasClosedDualityFor(commitmentId))
                continue;

            var contract = await contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken);
            if (contract is null)
                continue;

            // A perna Penalty é auto-liquidante (não há consumo recíproco): o agregado decide; o relay só fecha a
            // alocação contra o próprio evento e segue para a próxima perna.
            if (contract.TrySettlePenaltyCoverage(commitmentId, registeredEvent.Id, domainEvent.OccurredAt))
            {
                registeredEvent.CloseDualityAsSelfSettled(commitmentId, domainEvent.OccurredAt);
                continue;
            }

            var reciprocalCommitment = contract.FindReciprocalCommitment(commitmentId);
            var reciprocalEvent = await eventRepo.FindCoveredByCommitmentAsync(reciprocalCommitment.Id, tenantId, cancellationToken);
            if (reciprocalEvent is null)
                continue; // counterpart not registered yet — the close runs when its own event arrives

            DualityMatchingService.Match(registeredEvent, commitmentId, reciprocalEvent, reciprocalCommitment.Id, domainEvent.OccurredAt);
            MarkFulfilledIfPending(contract, commitmentId, registeredEvent.Id, domainEvent.OccurredAt);
            MarkFulfilledIfPending(contract, reciprocalCommitment.Id, reciprocalEvent.Id, domainEvent.OccurredAt);

            // A decisão de penalizar (direção, janela, idempotência, cálculo) é toda do agregado. O relay só
            // alimenta cada perna com a data do seu próprio evento; o método é no-op para a perna de inflow.
            contract.TryRegisterLatePenalty(commitmentId, PaidDateOf(registeredEvent), NewCommitmentId, domainEvent.OccurredAt);
            contract.TryRegisterLatePenalty(reciprocalCommitment.Id, PaidDateOf(reciprocalEvent), NewCommitmentId, domainEvent.OccurredAt);
        }
    }

    private static DateOnly PaidDateOf(EconomicEvent ev) => DateOnly.FromDateTime(ev.OccurredAt.InstantUtc);

    private static CommitmentId NewCommitmentId() => CommitmentId.From(Guid.CreateVersion7());

    private static void MarkFulfilledIfPending(
        EconomicContract contract, CommitmentId commitmentId, EconomicEventId fulfillingEventId, DateTime occurredAt)
    {
        var commitment = contract.FindCommitment(commitmentId);
        if (commitment.Status == CommitmentStatus.Fulfilled)
            return; // idempotent: a reprocessed relay message must not re-fulfill

        contract.MarkFulfilled(commitmentId, fulfillingEventId, occurredAt);
    }
}
