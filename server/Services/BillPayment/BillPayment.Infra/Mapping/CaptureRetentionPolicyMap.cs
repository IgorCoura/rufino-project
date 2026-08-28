namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.Retention;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CaptureRetentionPolicyMap : IEntityTypeConfiguration<CaptureRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<CaptureRetentionPolicy> builder)
    {
        builder.ToTable("capture_retention_policies");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CaptureRetentionPolicyId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();

        // O Id do Smart Enum É a quantidade de dias — a coluna se lê sozinha.
        builder.Property(e => e.Window)
            .HasColumnName("window_days")
            .HasConversion(w => w.Id, value => Enumeration.FromValue<RetentionWindow>(value))
            .IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Uma política por tenant, como o PayerProfile.
        builder.HasIndex(nameof(CaptureRetentionPolicy.TenantId))
            .IsUnique()
            .HasDatabaseName("ix_capture_retention_policies_tenant");
    }
}
