namespace BillPayment.Infra.Mapping;

using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PaymentWebhookEventMap : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.ToTable("payment_webhook_events");

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasMaxLength(PaymentWebhookEvent.EVENT_ID_MAX_LENGTH)
            .ValueGeneratedNever();

        builder.Property(e => e.ReceivedAt).HasColumnName("received_at").IsRequired();
    }
}
