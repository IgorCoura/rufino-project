namespace EconomicCore.Infra.Mapping;

using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OutboxDeadLetterMap : IEntityTypeConfiguration<OutboxDeadLetter>
{
    public void Configure(EntityTypeBuilder<OutboxDeadLetter> builder)
    {
        builder.ToTable("outbox_dead_letters");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(512).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.Attempts).HasColumnName("attempts").IsRequired();
        builder.Property(e => e.Error).HasColumnName("error").IsRequired();
        builder.Property(e => e.FailedAt).HasColumnName("failed_at").IsRequired();
    }
}
