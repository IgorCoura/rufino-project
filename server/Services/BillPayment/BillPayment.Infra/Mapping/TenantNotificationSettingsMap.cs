namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.Notifications;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TenantNotificationSettingsMap : IEntityTypeConfiguration<TenantNotificationSettings>
{
    public void Configure(EntityTypeBuilder<TenantNotificationSettings> builder)
    {
        builder.ToTable("tenant_notification_settings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TenantNotificationSettingsId.From(value))
            .ValueGeneratedNever();

        builder.Property(s => s.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(s => s.IsEnabled).HasColumnName("is_enabled").IsRequired();

        // Lista pequena e nunca filtrada em SQL: vai como jsonb, pelo mesmo motivo dos alertas do
        // ciclo e dos apelidos do Payee — tabela filha só quando houver consulta que precise dela.
        builder.Property(s => s.Recipients)
            .HasColumnName("recipients")
            .HasColumnType("jsonb")
            .HasConversion(
                recipients => JsonSerializer.Serialize(recipients, (JsonSerializerOptions?)null),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyCollection<string>>(
                    (a, b) => a!.SequenceEqual(b!, StringComparer.Ordinal),
                    a => a.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
                    a => a.ToList()))
            .IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Derivado — não é coluna. Ligá-lo ao banco criaria um segundo lugar para a regra "há
        // canal utilizável" envelhecer.
        builder.Ignore(s => s.CanDeliver);

        // Um por tenant, no mesmo molde do PayerProfile.
        builder.HasIndex(s => s.TenantId)
            .IsUnique()
            .HasDatabaseName("ix_tenant_notification_settings_tenant");
    }

    private static List<string> Deserialize(string json)
        => JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
