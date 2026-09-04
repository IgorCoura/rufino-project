namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TrustedOriginMap : IEntityTypeConfiguration<TrustedOrigin>
{
    public void Configure(EntityTypeBuilder<TrustedOrigin> builder)
    {
        builder.ToTable("trusted_origins");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TrustedOriginId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.Id, id => Enumeration.FromValue<OriginKind>(id))
            .IsRequired();

        builder.Property(e => e.Value)
            .HasColumnName("value")
            .HasMaxLength(TrustedOrigin.VALUE_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Decision)
            .HasColumnName("decision")
            .HasConversion(d => d.Id, id => Enumeration.FromValue<TrustDecision>(id))
            .IsRequired();

        builder.Property(e => e.DecidedBy)
            .HasColumnName("decided_by")
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.DecidedAt).HasColumnName("decided_at").IsRequired();
        builder.Property(e => e.Note).HasColumnName("note").HasMaxLength(TrustedOrigin.NOTE_MAX_LENGTH);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Invariante BLP.ORG01. O banco é quem garante sob concorrência — a checagem no
        // handler evita o round-trip no caso comum, mas não resolve a corrida.
        builder.HasIndex(e => new { e.TenantId, e.Kind, e.Value })
            .IsUnique()
            .HasDatabaseName("ix_trusted_origins_tenant_kind_value");

        // Cobre a resolução por remetente, que filtra por tenant e valor normalizado.
        builder.HasIndex(e => new { e.TenantId, e.Value })
            .HasDatabaseName("ix_trusted_origins_tenant_value");
    }
}
