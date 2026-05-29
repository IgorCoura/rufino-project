namespace EconomicCore.Infra.Mapping;

using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OutboxMessageMap : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(512).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.Processed).HasColumnName("processed").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        builder.Property(e => e.Attempts).HasColumnName("attempts").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.Error).HasColumnName("error");

        // Partial index over the claim predicate (unprocessed, oldest-first) — keeps the polling query cheap as the table grows.
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_outbox_messages_unprocessed")
            .HasFilter("processed = false");
    }
}
