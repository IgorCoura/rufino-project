namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PayeeMap : IEntityTypeConfiguration<Payee>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Payee> builder)
    {
        builder.ToTable("payees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PayeeId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(Payee.LEGAL_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(TaxIdConversions.MAX_LENGTH)
            .HasConversion(TaxIdConversions.Single, TaxIdConversions.SingleComparer)
            .IsRequired();

        // AmountPolicy vira UMA coluna jsonb em vez de owned type. O motivo é a lição do
        // EconomicCore registrada no CLAUDE.md: owned de 2º nível (os Money dentro da
        // política) anexado a agregado já persistido não é rastreado e grava NULL — e
        // ChangeAmountPolicy faz exatamente isso. A rehidratação passa pelas factories
        // públicas, então dado corrompido falha alto em vez de virar política inválida.
        builder.Property(e => e.AmountPolicy)
            .HasColumnName("amount_policy")
            .HasColumnType("jsonb")
            .HasConversion(
                policy => Serialize(policy),
                json => Deserialize(json),
                new ValueComparer<AmountPolicy>(
                    (left, right) => left == null ? right == null : left.Equals(right),
                    policy => policy.GetHashCode(),
                    policy => policy))
            .IsRequired();

        // Listas de valor simples também em jsonb: são lidas junto com o agregado e
        // nunca filtradas em SQL, então tabela filha só custaria join.
        builder.Property(e => e.Aliases)
            .HasColumnName("aliases")
            .HasColumnType("jsonb")
            .HasConversion(
                aliases => JsonSerializer.Serialize(aliases, Json),
                json => JsonSerializer.Deserialize<List<string>>(json, Json)!,
                new ValueComparer<IReadOnlyCollection<string>>(
                    (left, right) => left!.SequenceEqual(right!, StringComparer.Ordinal),
                    values => values.Aggregate(0, (hash, v) => HashCode.Combine(hash, v.GetHashCode(StringComparison.Ordinal))),
                    values => values.ToList()))
            .IsRequired();

        builder.Property(e => e.AcceptedBanks)
            .HasColumnName("accepted_banks")
            .HasColumnType("jsonb")
            .HasConversion(
                banks => JsonSerializer.Serialize(banks.Select(b => b.Value).ToList(), Json),
                json => JsonSerializer.Deserialize<List<string>>(json, Json)!.ConvertAll(code => new BankCode(code)),
                new ValueComparer<IReadOnlyCollection<BankCode>>(
                    (left, right) => left!.SequenceEqual(right!),
                    banks => banks.Aggregate(0, (hash, b) => HashCode.Combine(hash, b.GetHashCode())),
                    banks => banks.ToList()))
            .IsRequired();

        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Invariante BLP.PYE01: um beneficiário por documento em cada tenant. O índice é
        // quem garante sob concorrência; a checagem no handler só evita o caso comum.
        builder.HasIndex(e => new { e.TenantId, e.TaxId })
            .IsUnique()
            .HasDatabaseName("ix_payees_tenant_tax_id");
    }

    private static string Serialize(AmountPolicy policy)
        => JsonSerializer.Serialize(
            new AmountPolicyRecord(
                policy.Kind.Id,
                policy.ExpectedAmount?.Amount,
                policy.ExpectedAmount?.Currency.Id,
                policy.TolerancePercent,
                policy.MinAmount?.Amount,
                policy.MaxAmount?.Amount,
                policy.MinAmount?.Currency.Id),
            Json);

    private static AmountPolicy Deserialize(string json)
    {
        var record = JsonSerializer.Deserialize<AmountPolicyRecord>(json, Json)
            ?? throw new InvalidOperationException("Política de valor do beneficiário ilegível.");

        var kind = Enumeration.FromValue<AmountPolicyKind>(record.Kind);

        if (kind == AmountPolicyKind.Fixed)
            return AmountPolicy.Fixed(
                new Money(record.ExpectedAmount!.Value, Enumeration.FromValue<Currency>(record.ExpectedCurrency!.Value)),
                record.TolerancePercent!.Value);

        if (kind == AmountPolicyKind.Range)
        {
            var currency = Enumeration.FromValue<Currency>(record.RangeCurrency!.Value);
            return AmountPolicy.Range(
                new Money(record.MinAmount!.Value, currency),
                new Money(record.MaxAmount!.Value, currency));
        }

        return AmountPolicy.Unbounded();
    }

    private sealed record AmountPolicyRecord(
        int Kind,
        decimal? ExpectedAmount,
        int? ExpectedCurrency,
        decimal? TolerancePercent,
        decimal? MinAmount,
        decimal? MaxAmount,
        int? RangeCurrency);
}
