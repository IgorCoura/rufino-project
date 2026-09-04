namespace BillPayment.Infra.Mapping;

using System.Globalization;
using System.Text.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PaymentOrderMap : IEntityTypeConfiguration<PaymentOrder>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("payment_orders");

        // Token de concorrência otimista, como nos demais agregados: webhook e worker de
        // submissão podem disputar a mesma linha, e o perdedor recebe 409/reprocesso — nunca
        // um estado costurado de duas escritas.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PaymentOrderId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.BillId)
            .HasColumnName("bill_id")
            .HasConversion(id => id.Value, value => BillId.From(value))
            .IsRequired();

        builder.Property(e => e.Rail)
            .HasColumnName("rail")
            .HasConversion(r => r.Id, id => Enumeration.FromValue<PaymentRail>(id))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<PaymentOrderStatus>(id))
            .IsRequired();

        builder.Property(e => e.Hold)
            .HasColumnName("hold")
            .HasConversion(h => h.Id, id => Enumeration.FromValue<PaymentOrderHold>(id))
            .IsRequired();

        builder.Property(e => e.RequestedScheduleDate)
            .HasColumnName("requested_schedule_date")
            .IsRequired();

        builder.Property(e => e.EffectiveScheduleDate).HasColumnName("effective_schedule_date");

        builder.Property(e => e.ProviderOrderId)
            .HasColumnName("provider_order_id")
            .HasMaxLength(PaymentOrder.PROVIDER_ORDER_ID_MAX_LENGTH);

        // Money é owned de 1º nível com escalares — pode ser achatado com segurança (a
        // armadilha documentada é o owned de 2º nível).
        builder.OwnsOne(e => e.Amount, amount =>
        {
            amount.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2);

            amount.Property(m => m.Currency)
                .HasColumnName("amount_currency")
                .HasConversion(c => c.Id, id => Enumeration.FromValue<Currency>(id));
        });

        builder.OwnsOne(e => e.Fee, fee =>
        {
            fee.Property(m => m.Amount)
                .HasColumnName("fee")
                .HasPrecision(18, 2);

            fee.Property(m => m.Currency)
                .HasColumnName("fee_currency")
                .HasConversion(c => c.Id, id => Enumeration.FromValue<Currency>(id));
        });

        builder.Property(e => e.PaidAt).HasColumnName("paid_at");

        builder.Property(e => e.FailReasons)
            .HasColumnName("fail_reasons")
            .HasColumnType("jsonb")
            .HasConversion(
                reasons => JsonSerializer.Serialize(reasons, Json),
                json => JsonSerializer.Deserialize<List<string>>(json, Json) ?? new List<string>(),
                new ValueComparer<IReadOnlyCollection<string>>(
                    (left, right) => left!.SequenceEqual(right!),
                    values => values.Aggregate(0, (hash, r) => HashCode.Combine(hash, r.GetHashCode(StringComparison.Ordinal))),
                    values => values.ToList()))
            .IsRequired();

        builder.Property(e => e.LastProviderSyncAt).HasColumnName("last_provider_sync_at");

        builder.Property(e => e.ReceiptStorageKey)
            .HasColumnName("receipt_storage_key")
            .HasMaxLength(PaymentOrder.RECEIPT_STORAGE_KEY_MAX_LENGTH);

        builder.Property(e => e.ReceiptUnavailable)
            .HasColumnName("receipt_unavailable")
            .HasDefaultValue(false)
            .IsRequired();

        // Carimbada pelo claim ADO das varreduras (conciliação e rede de segurança do
        // comprovante) — o agregado só a lê; é o anti-inanição do lote.
        builder.Property(e => e.SweepAttemptedAt).HasColumnName("sweep_attempted_at");

        builder.Property(e => e.SubmissionAttempts).HasColumnName("submission_attempts").IsRequired();
        builder.Property(e => e.SubmissionLeaseExpiresAt).HasColumnName("submission_lease_expires_at");

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(PaymentOrder.LAST_ERROR_MAX_LENGTH);

        builder.Property(e => e.ConfirmedBy)
            .HasColumnName("confirmed_by")
            .HasConversion(id => id!.Value.Value, value => UserId.From(value));

        builder.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Derivados — não são colunas. ExternalReference É o id; materializá-lo criaria o
        // caminho em que os dois divergem.
        builder.Ignore(e => e.ExternalReference);
        builder.Ignore(e => e.HasImmediateExecutionConsent);

        builder.HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("ix_payment_orders_tenant_created");

        // Uma ordem ATIVA por boleto: é o que torna idempotente o handler de aprovação sob a
        // entrega at-least-once do outbox — a segunda entrega encontra a ordem da primeira, e
        // uma corrida entre duas entregas morre aqui no banco, não numa checagem de memória.
        builder.HasIndex(e => e.BillId)
            .IsUnique()
            .HasFilter(BuildActiveOrderFilter())
            .HasDatabaseName("ix_payment_orders_bill_active");

        // A fila de submissão: só Draft interessa, e o WHERE da reivindicação filtra por hold
        // e aluguel. Parcial porque a coluna é altamente seletiva.
        builder.HasIndex(e => new { e.Status, e.Hold, e.SubmissionLeaseExpiresAt })
            .HasDatabaseName("ix_payment_orders_submission_queue")
            .HasFilter("status = 1");
    }

    private static string BuildActiveOrderFilter()
    {
        // Derivado do Smart Enum para o filtro acompanhar a semântica de IsTerminal.
        var terminal = Enumeration.GetAll<PaymentOrderStatus>()
            .Where(s => s.IsTerminal)
            .Select(s => s.Id.ToString(CultureInfo.InvariantCulture));

        return $"\"status\" NOT IN ({string.Join(", ", terminal)})";
    }
}
