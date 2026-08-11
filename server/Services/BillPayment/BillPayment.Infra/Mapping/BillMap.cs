namespace BillPayment.Infra.Mapping;

using System.Globalization;
using System.Text.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BillMap : IEntityTypeConfiguration<Bill>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("bills");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => BillId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<BillStatus>(id))
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.Id, id => Enumeration.FromValue<BillKind>(id))
            .IsRequired();

        builder.Property(e => e.Rail)
            .HasColumnName("rail")
            .HasConversion(r => r.Id, id => Enumeration.FromValue<PaymentRail>(id))
            .IsRequired();

        // Origem é owned de primeiro nível e sem VO aninhado — pode ser achatada com
        // segurança, e ter as colunas em SQL serve ao check de origem confiável.
        builder.OwnsOne(e => e.Origin, origin =>
        {
            origin.Property(o => o.SourceKind)
                .HasColumnName("origin_source_kind")
                .HasConversion(k => k.Id, id => Enumeration.FromValue<BillSourceKind>(id))
                .IsRequired();

            origin.Property(o => o.SourceId).HasColumnName("origin_source_id");
            origin.Property(o => o.ReceivedAt).HasColumnName("origin_received_at").IsRequired();

            origin.Property(o => o.SenderAddress)
                .HasColumnName("origin_sender_address")
                .HasMaxLength(BillOrigin.SENDER_ADDRESS_MAX_LENGTH);

            origin.Property(o => o.ExternalMessageId)
                .HasColumnName("origin_external_message_id")
                .HasMaxLength(BillOrigin.EXTERNAL_MESSAGE_ID_MAX_LENGTH);

            origin.Property(o => o.ContentHash)
                .HasColumnName("origin_content_hash")
                .HasMaxLength(BillOrigin.CONTENT_HASH_MAX_LENGTH);

            origin.Property(o => o.StorageKey)
                .HasColumnName("origin_storage_key")
                .HasMaxLength(BillOrigin.STORAGE_KEY_MAX_LENGTH);

            origin.HasIndex(o => o.SenderAddress).HasDatabaseName("ix_bills_origin_sender");
        });
        builder.Navigation(e => e.Origin).IsRequired();

        // Instrumentos em jsonb, não em tabela filha: eles contêm VOs aninhados
        // (DigitableLine/PixPayload/Money) e owned de 2º nível grava NULL ao mutar agregado
        // já persistido — a lição do EconomicCore registrada no CLAUDE.md. Nunca são
        // filtrados em SQL; quem responde por unicidade é a coluna dedup_key.
        builder.Property(e => e.Instruments)
            .HasColumnName("instruments")
            .HasColumnType("jsonb")
            .HasConversion(
                instruments => Serialize(instruments),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyCollection<PaymentInstrument>>(
                    (left, right) => left!.SequenceEqual(right!),
                    values => values.Aggregate(0, (hash, i) => HashCode.Combine(hash, i.GetHashCode())),
                    values => values.ToList()))
            .IsRequired();

        builder.Property(e => e.DedupKey)
            .HasColumnName("dedup_key")
            .HasMaxLength(128);

        builder.Property(e => e.PayeeId)
            .HasColumnName("payee_id")
            .HasConversion(id => id!.Value.Value, value => PayeeId.From(value));

        builder.Property(e => e.Routing)
            .HasColumnName("routing_confidence")
            .HasConversion(r => r!.Id, id => Enumeration.FromValue<RoutingConfidence>(id));

        // Pagador extraído: owned de 1º nível, e o TaxId dentro dele é coluna de texto via
        // conversor (não outro owned), então não recai na armadilha de 2º nível.
        builder.OwnsOne(e => e.ExtractedPayer, payer =>
        {
            payer.Property(p => p.Name)
                .HasColumnName("extracted_payer_name")
                .HasMaxLength(PartyInfo.NAME_MAX_LENGTH);

            payer.Property(p => p.TaxId)
                .HasColumnName("extracted_payer_tax_id")
                .HasMaxLength(TaxIdConversions.MAX_LENGTH)
                .HasConversion(TaxIdConversions.Single!, TaxIdConversions.SingleComparer!);
        });

        // Retratos e histórico em jsonb pelo mesmo motivo dos instrumentos — ver LookupConversions.
        builder.Property(e => e.Lookup)
            .HasColumnName("lookup")
            .HasColumnType("jsonb")
            .HasConversion(LookupConversions.BankSlip, LookupConversions.BankSlipComparer);

        builder.Property(e => e.PixLookup)
            .HasColumnName("pix_lookup")
            .HasColumnType("jsonb")
            .HasConversion(LookupConversions.Pix, LookupConversions.PixComparer);

        builder.Property(e => e.LookupHistory)
            .HasColumnName("lookup_history")
            .HasColumnType("jsonb")
            .HasConversion(LookupConversions.History, LookupConversions.HistoryComparer)
            .IsRequired();

        // Checks vão para tabela filha (ADR-003) e não para jsonb: só contêm escalares, então
        // são owned de 1º nível e o EF os rastreia sem o problema do 2º nível. A tabela também
        // é o que permite, depois, uma fila operacional filtrada por motivo em SQL.
        builder.OwnsMany(e => e.Checks, check =>
        {
            check.ToTable("bill_checks");
            check.WithOwner().HasForeignKey("bill_id");

            check.Property(c => c.Type)
                .HasColumnName("type")
                .HasConversion(t => t.Id, id => Enumeration.FromValue<CheckType>(id))
                .IsRequired();

            check.Property(c => c.Outcome)
                .HasColumnName("outcome")
                .HasConversion(o => o.Id, id => Enumeration.FromValue<CheckOutcome>(id))
                .IsRequired();

            check.Property(c => c.Severity)
                .HasColumnName("severity")
                .HasConversion(s => s.Id, id => Enumeration.FromValue<CheckSeverity>(id))
                .IsRequired();

            check.Property(c => c.ReasonCode)
                .HasColumnName("reason_code")
                .HasMaxLength(CheckResult.REASON_CODE_MAX_LENGTH);

            check.Property(c => c.Evidence)
                .HasColumnName("evidence")
                .HasMaxLength(CheckResult.EVIDENCE_MAX_LENGTH);

            check.Property(c => c.EvaluatedAt).HasColumnName("evaluated_at").IsRequired();

            // Uma verificação por tipo, por boleto — é o que RecordChecks garante no domínio e
            // o que a chave torna impossível de furar por outro caminho.
            check.HasKey("bill_id", nameof(BillCheck.Type));
        });

        builder.Property(e => e.ScheduledFor).HasColumnName("scheduled_for");

        // A decisão é owned de 1º nível e só tem escalares — pode ser achatada com segurança.
        // Ter as colunas em SQL é o que permite o relatório responder "quem aprovou o quê".
        builder.OwnsOne(e => e.Approval, approval =>
        {
            approval.Property(a => a.DecidedBy)
                .HasColumnName("approval_decided_by")
                .HasConversion(id => id.Value, value => UserId.From(value));

            approval.Property(a => a.Decision)
                .HasColumnName("approval_decision")
                .HasConversion(d => d.Id, id => Enumeration.FromValue<ApprovalDecision>(id));

            approval.Property(a => a.DecidedAt).HasColumnName("approval_decided_at");

            approval.Property(a => a.Note)
                .HasColumnName("approval_note")
                .HasMaxLength(ApprovalRecord.NOTE_MAX_LENGTH);
        });

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.CreatedAt }).HasDatabaseName("ix_bills_tenant_created");

        // Invariante BLP.BIL02 — unicidade GLOBAL da chave de instrumento.
        //
        // NÃO acrescente TenantId a este índice. É deliberado: um compromisso é pago uma vez,
        // e uma caixa de e-mail compartilhada entre tenants torna a colisão provável. É uma
        // das três travessias de tenant autorizadas do BC (ADR-008).
        //
        // O filtro exclui os status que liberam a chave — Denied e Cancelled — porque o
        // compromisso não vai ser pago por eles e o documento pode ser reimportado.
        builder.HasIndex(e => e.DedupKey)
            .IsUnique()
            .HasFilter(BuildActiveDedupFilter())
            .HasDatabaseName("ix_bills_dedup_key_active");
    }

    private static string BuildActiveDedupFilter()
    {
        // Construído a partir do Smart Enum para o filtro não divergir do domínio se a
        // semântica de OccupiesNaturalKey mudar.
        var releasing = Enumeration.GetAll<BillStatus>()
            .Where(s => !s.OccupiesNaturalKey)
            .Select(s => s.Id.ToString(CultureInfo.InvariantCulture));

        return $"\"dedup_key\" IS NOT NULL AND \"status\" NOT IN ({string.Join(", ", releasing)})";
    }

    private static string Serialize(IReadOnlyCollection<PaymentInstrument> instruments)
        => JsonSerializer.Serialize(
            instruments.Select(i => new InstrumentRecord(
                Kind: i.Kind.Id,
                Content: i.Kind == PaymentInstrumentKind.Barcode ? i.DigitableLine.Value : i.PixPayload.Payload,

                // O fator de vencimento tem 4 dígitos e já deu a volta uma vez, então a
                // mesma linha resolve para duas datas possíveis dependendo da referência.
                // Gravar o vencimento que foi calculado na captura e reusá-lo como âncora
                // faz a releitura reproduzi-lo exatamente: ele é um dos candidatos e fica a
                // distância zero de si mesmo, então vence sempre. Sem isso, reler um boleto
                // de 2026 daqui a dez anos poderia escolher a outra época.
                DueDateAnchor: i.Kind == PaymentInstrumentKind.Barcode
                    ? i.DigitableLine.DueDate
                    : null))
            .ToList(),
            Json);

    private static List<PaymentInstrument> Deserialize(string json)
    {
        var records = JsonSerializer.Deserialize<List<InstrumentRecord>>(json, Json)
            ?? throw new InvalidOperationException("Instrumentos de pagamento ilegíveis.");

        return records.ConvertAll(r =>
            Enumeration.FromValue<PaymentInstrumentKind>(r.Kind) == PaymentInstrumentKind.Barcode
                // Sem âncora o fator era zero (documento sem vencimento) e a releitura
                // devolve null de novo, qualquer que seja a referência.
                ? PaymentInstrument.FromBarcode(DigitableLine.Parse(r.Content, r.DueDateAnchor ?? DateTime.UnixEpoch))
                : PaymentInstrument.FromPixQr(PixPayload.Parse(r.Content)));
    }

    private sealed record InstrumentRecord(int Kind, string Content, DateTime? DueDateAnchor);
}
