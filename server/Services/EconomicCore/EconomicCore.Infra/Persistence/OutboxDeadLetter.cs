namespace EconomicCore.Infra.Persistence;

public sealed class OutboxDeadLetter
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int Attempts { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public DateTime FailedAt { get; private set; }

    private OutboxDeadLetter() { }

    public static OutboxDeadLetter From(OutboxMessage message, DateTime failedAt)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new OutboxDeadLetter
        {
            Id = message.Id,
            EventType = message.EventType,
            Payload = message.Payload,
            OccurredAt = message.OccurredAt,
            CreatedAt = message.CreatedAt,
            Attempts = message.Attempts,
            Error = message.Error ?? string.Empty,
            FailedAt = failedAt,
        };
    }
}
