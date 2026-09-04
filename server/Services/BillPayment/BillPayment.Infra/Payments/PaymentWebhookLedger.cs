namespace BillPayment.Infra.Payments;

using BillPayment.Domain.SeedWork;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A idempotência do webhook sobre <c>payment_webhook_events</c> — molde do
/// <c>RequestManager</c>: <see cref="RecordAsync"/> só registra no contexto, e a marca persiste
/// junto com o efeito no <c>SaveEntitiesAsync</c> do handler.
/// </summary>
internal sealed class PaymentWebhookLedger(BillPaymentDbContext context) : IPaymentWebhookLedger
{
    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default)
        => context.PaymentWebhookEvents.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);

    public async Task RecordAsync(string eventId, DateTime receivedAt, CancellationToken cancellationToken = default)
        => await context.PaymentWebhookEvents.AddAsync(new PaymentWebhookEvent(eventId, receivedAt), cancellationToken);
}
