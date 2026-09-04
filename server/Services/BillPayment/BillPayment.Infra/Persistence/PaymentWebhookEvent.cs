namespace BillPayment.Infra.Persistence;

/// <summary>
/// A marca de idempotência de um evento de webhook do provedor — plataforma, sem Aggregate,
/// como <see cref="ClientRequest"/>. O id é o do PROVEDOR (string opaca), e a PK é o que faz
/// duas entregas concorrentes colidirem no banco em vez de produzirem efeito dobrado.
/// </summary>
public sealed class PaymentWebhookEvent
{
    public const int EVENT_ID_MAX_LENGTH = 100;

    public string EventId { get; private set; } = string.Empty;

    public DateTime ReceivedAt { get; private set; }

    private PaymentWebhookEvent() { }

    public PaymentWebhookEvent(string eventId, DateTime receivedAt)
    {
        EventId = eventId;
        ReceivedAt = receivedAt;
    }
}
