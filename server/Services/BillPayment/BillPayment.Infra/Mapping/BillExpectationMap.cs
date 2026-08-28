namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BillExpectationMap : IEntityTypeConfiguration<BillExpectation>
{
    public void Configure(EntityTypeBuilder<BillExpectation> builder)
    {
        builder.ToTable("bill_expectations");

        // xmin como token de concorrência (ver BillMap): a varredura e o cumprimento pelo boleto
        // não gravam um em cima do outro.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => BillExpectationId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.PayeeId)
            .HasColumnName("payee_id")
            .HasConversion(id => id.Value, value => PayeeId.From(value))
            .IsRequired();

        // String vazia, nunca NULL: a coluna entra num índice único, e no Postgres NULL não
        // colide com NULL — duas expectativas sem referência de conta passariam pelo banco.
        builder.Property(e => e.AccountReference)
            .HasColumnName("account_reference")
            .HasMaxLength(BillExpectation.ACCOUNT_REFERENCE_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Label)
            .HasColumnName("label")
            .HasMaxLength(BillExpectation.LABEL_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Recurrence)
            .HasColumnName("recurrence")
            .HasConversion(r => r.Id, id => Enumeration.FromValue<Recurrence>(id))
            .IsRequired();

        builder.Property(e => e.Origin)
            .HasColumnName("origin")
            .HasConversion(o => o.Id, id => Enumeration.FromValue<ExpectationOrigin>(id))
            .IsRequired();

        builder.Property(e => e.ExpectedDueDay).HasColumnName("expected_due_day").IsRequired();
        builder.Property(e => e.ObservedLeadDays).HasColumnName("observed_lead_days").IsRequired();
        builder.Property(e => e.AlertLeadDays).HasColumnName("alert_lead_days").IsRequired();
        builder.Property(e => e.ObservationCount).HasColumnName("observation_count").IsRequired();

        // Achatada em UM inteiro (ano*100+mês), como a competência do ciclo — pelo mesmo motivo:
        // owned de 2º nível sob agregado já persistido não é rastreado e grava NULL.
        builder.Property(e => e.AnchorCompetence)
            .HasColumnName("anchor_competence")
            .HasConversion(
                p => (p.Year * 100) + p.Month,
                v => new CompetencePeriod(v / 100, v % 100))
            .IsRequired();

        builder.Property(e => e.WatchingSince).HasColumnName("watching_since").IsRequired();
        builder.Property(e => e.LastSweptAt).HasColumnName("last_swept_at").IsRequired();

        builder.Property(e => e.HintSourceId)
            .HasColumnName("hint_source_id")
            .HasConversion(
                id => id!.Value.Value,
                value => CaptureSourceId.From(value));

        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.PausedUntil).HasColumnName("paused_until");

        builder.Property(e => e.DeactivationReason)
            .HasColumnName("deactivation_reason")
            .HasMaxLength(BillExpectation.DEACTIVATION_REASON_MAX_LENGTH);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.OwnsMany(e => e.Cycles, ConfigureCycles);

        // Invariante BLP.EXP01. A referência de conta faz parte da chave porque um tenant tem
        // várias contas do mesmo beneficiário — medido: quatro instalações da EDP, três do DAE.
        builder.HasIndex(e => new { e.TenantId, e.PayeeId, e.AccountReference })
            .IsUnique()
            .HasDatabaseName("ix_bill_expectations_tenant_payee_account");

        // Cobre a fila do job. O carimbo é o de VARREDURA, não o de atualização de negócio:
        // ordenar por `updated_at` invertia a prioridade da fila — expectativa parada mantinha o
        // carimbo antigo e ocupava as vagas do lote para sempre, enquanto a que estava sendo
        // cumprida ia para o fim e, passando do tamanho do lote, nunca mais era varrida.
        builder.HasIndex(e => new { e.IsActive, e.LastSweptAt })
            .HasDatabaseName("ix_bill_expectations_active_swept");

        // Serve ao alerta de "chegou e não consegui ler": o artefato travado não tem beneficiário
        // nem vencimento, e a fonte é a única coisa que o liga a uma conta esperada. Parcial
        // porque a coluna é nula na maioria das linhas — e é justamente a minoria que se busca.
        builder.HasIndex(e => new { e.TenantId, e.HintSourceId })
            .HasDatabaseName("ix_bill_expectations_tenant_hint_source")
            .HasFilter("hint_source_id IS NOT NULL");
    }

    private static void ConfigureCycles(OwnedNavigationBuilder<BillExpectation, ExpectationCycle> cycles)
    {
        cycles.ToTable("bill_expectation_cycles");

        // A FK sombra tem que ter o MESMO tipo da chave da raiz — BillExpectationId, não Guid.
        // Declarada como Guid, o EF recusa o modelo inteiro na validação e derruba a suíte.
        cycles.Property<BillExpectationId>("bill_expectation_id")
            .HasColumnName("bill_expectation_id")
            .HasConversion(id => id.Value, value => BillExpectationId.From(value));

        cycles.WithOwner().HasForeignKey("bill_expectation_id");

        cycles.HasKey(c => c.Id);
        cycles.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ExpectationCycleId.From(value))
            .ValueGeneratedNever();

        // A competência é achatada em duas colunas escalares em vez de virar owned de 2º nível:
        // owned aninhado sob agregado já persistido não é rastreado e grava NULL (lição do
        // EconomicCore, registrada no CLAUDE.md).
        cycles.Property(c => c.Competence)
            .HasColumnName("competence")
            .HasConversion(
                p => p.Year * 100 + p.Month,
                v => new CompetencePeriod(v / 100, v % 100))
            .IsRequired();

        cycles.Property(c => c.ExpectedDueDate).HasColumnName("expected_due_date").IsRequired();
        cycles.Property(c => c.AlertAt).HasColumnName("alert_at").IsRequired();

        cycles.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<CycleStatus>(id))
            .IsRequired();

        cycles.Property(c => c.FulfilledByBillId)
            .HasColumnName("fulfilled_by_bill_id")
            .HasConversion(id => id!.Value.Value, value => BillId.From(value));

        cycles.Property(c => c.BlockedByCaptureItemId)
            .HasColumnName("blocked_by_capture_item_id")
            .HasConversion(id => id!.Value.Value, value => CaptureItemId.From(value));

        cycles.Property(c => c.MissReason)
            .HasColumnName("miss_reason")
            .HasConversion(r => r!.Id, id => Enumeration.FromValue<MissReason>(id));

        cycles.Property(c => c.WaivedBy)
            .HasColumnName("waived_by")
            .HasConversion(id => id!.Value.Value, value => UserId.From(value));

        cycles.Property(c => c.WaiveReason)
            .HasColumnName("waive_reason")
            .HasMaxLength(ExpectationCycle.WAIVE_REASON_MAX_LENGTH);

        // Coleção pequena e nunca filtrada em SQL: vai como jsonb, pelo mesmo motivo dos
        // retratos de consulta — tabela filha só quando houver consulta que precise dela.
        cycles.Property(c => c.Alerts)
            .HasColumnName("alerts")
            .HasColumnType("jsonb")
            .HasConversion(
                alerts => Serialize(alerts),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyCollection<AlertRecord>>(
                    (a, b) => a!.SequenceEqual(b!),
                    a => a.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    a => a.ToList()))
            .IsRequired();

        cycles.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        cycles.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Invariante BLP.EXP02: um ciclo por competência. O banco é quem garante sob concorrência
        // — o job pode rodar em dois deployments antes de alguém lembrar de ligá-lo em um só.
        cycles.HasIndex("bill_expectation_id", nameof(ExpectationCycle.Competence))
            .IsUnique()
            .HasDatabaseName("ix_bill_expectation_cycles_expectation_competence");

        // O caminho de volta: o item travado foi resolvido e o ciclo precisa deixar de apontar
        // para ele. Parcial pelo mesmo motivo do índice de fonte — a coluna é nula quase sempre.
        cycles.HasIndex(c => c.BlockedByCaptureItemId)
            .HasDatabaseName("ix_bill_expectation_cycles_blocked_item")
            .HasFilter("blocked_by_capture_item_id IS NOT NULL");
    }

    /// <summary>
    /// A desserialização passa pela factory pública do domínio, nunca por construtor privado —
    /// nível de alerta inválido no banco falha alto na leitura em vez de virar registro mudo.
    /// </summary>
    private static List<AlertRecord> Deserialize(string json)
    {
        var rows = JsonSerializer.Deserialize<List<AlertRow>>(json) ?? [];

        return rows.ConvertAll(r => AlertRecord.Of(Enumeration.FromValue<AlertLevel>(r.Level), r.SentAt));
    }

    private static string Serialize(IReadOnlyCollection<AlertRecord> alerts)
        => JsonSerializer.Serialize(alerts.Select(a => new AlertRow(a.Level.Id, a.SentAt)).ToList());

    private sealed record AlertRow(int Level, DateTime SentAt);
}
