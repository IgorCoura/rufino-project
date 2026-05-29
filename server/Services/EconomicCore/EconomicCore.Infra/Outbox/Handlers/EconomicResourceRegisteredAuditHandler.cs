namespace EconomicCore.Infra.Outbox.Handlers;

using EconomicCore.Domain.Operational.EconomicResources.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Infra.Persistence;

internal sealed class EconomicResourceRegisteredAuditHandler(EconomicCoreDbContext db, TimeProvider timeProvider)
    : IDomainEventHandler<EconomicResourceRegistered>
{
    public Task HandleAsync(EconomicResourceRegistered domainEvent, CancellationToken cancellationToken = default)
    {
        db.ProcessedEventLogs.Add(new ProcessedEventLog(
            domainEvent.EventId,
            domainEvent.ResourceId.Value,
            domainEvent.Name,
            domainEvent.OccurredAt,
            timeProvider.GetUtcNow().UtcDateTime));

        // No SaveChanges here — the OutboxProcessor persists this insert in the same transaction
        // that marks the outbox message processed (effectively-once for same-DB side effects).
        return Task.CompletedTask;
    }
}
