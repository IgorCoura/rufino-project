namespace EconomicCore.Infra.Mapping;

using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ProcessedEventLogMap : IEntityTypeConfiguration<ProcessedEventLog>
{
    public void Configure(EntityTypeBuilder<ProcessedEventLog> builder)
    {
        builder.ToTable("processed_event_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();
    }
}
