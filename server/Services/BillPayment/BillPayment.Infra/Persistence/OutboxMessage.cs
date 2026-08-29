namespace BillPayment.Infra.Persistence;

using System.Text.Json;
using BillPayment.Domain.SeedWork;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }

    /// <summary>
    /// Antes deste instante a mensagem não é reivindicada de novo. Backoff exponencial gravado
    /// na linha (2026-08-28): sem ele o relay reivindicava a mesma mensagem no ciclo seguinte, e
    /// trinta segundos de provedor fora bastavam para esgotar as tentativas e mandar para
    /// dead-letter o que uma espera resolveria.
    /// </summary>
    public DateTime? NextAttemptAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage From(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var type = domainEvent.GetType();
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            // FullName (not Name) so the consumer can resolve the concrete CLR type unambiguously on deserialization.
            EventType = type.FullName!,
            Payload = JsonSerializer.Serialize(domainEvent, type),
            OccurredAt = domainEvent.OccurredAt,
            CreatedAt = DateTime.UtcNow,
            Processed = false,
            Attempts = 0,
        };
    }

    public void MarkProcessed(DateTime processedAt)
    {
        Processed = true;
        ProcessedAt = processedAt;
        Error = null;
    }

    public void RegisterFailure(string error, DateTime nextAttemptAt)
    {
        Attempts++;
        Error = error;
        NextAttemptAt = nextAttemptAt;
    }
}
