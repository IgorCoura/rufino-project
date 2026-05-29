namespace EconomicCore.Infra.Persistence;

// Read-model projection written by a domain-event handler — proves the outbox pipeline end-to-end
// and serves as the template for future projections fed off the outbox.
public sealed class ProcessedEventLog
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedEventLog() { }

    public ProcessedEventLog(Guid id, Guid resourceId, string name, DateTime occurredAt, DateTime processedAt)
    {
        Id = id;
        ResourceId = resourceId;
        Name = name;
        OccurredAt = occurredAt;
        ProcessedAt = processedAt;
    }
}
