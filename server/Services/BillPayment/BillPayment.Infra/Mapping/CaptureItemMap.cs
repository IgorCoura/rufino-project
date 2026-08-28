namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CaptureItemMap : IEntityTypeConfiguration<CaptureItem>
{
    public void Configure(EntityTypeBuilder<CaptureItem> builder)
    {
        builder.ToTable("capture_items");

        // xmin como token de concorrência: reivindicação e processamento não gravam um em cima do
        // outro (ver o comentário em BillMap). A reivindicação da fila é SQL cru e não passa por
        // aqui — o token protege as mutações pelo change tracker.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CaptureItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        // Referência a outro Aggregate: só o id, sem navigation property e sem FK
        // (as constraints descobertas por convenção são removidas no OnModelCreating).
        builder.Property(e => e.SourceId)
            .HasColumnName("source_id")
            .HasConversion(id => id.Value, value => CaptureSourceId.From(value))
            .IsRequired();

        builder.Property(e => e.ExternalMessageId)
            .HasColumnName("external_message_id")
            .HasMaxLength(CaptureItem.EXTERNAL_MESSAGE_ID_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.InternetMessageId)
            .HasColumnName("internet_message_id")
            .HasMaxLength(CaptureItem.INTERNET_MESSAGE_ID_MAX_LENGTH);

        builder.Property(e => e.ArtifactKey)
            .HasColumnName("artifact_key")
            .HasMaxLength(CaptureItem.ARTIFACT_KEY_MAX_LENGTH)
            .IsRequired();

        // Tipo declarado pelo provedor. Guardado porque artifact_key NÃO é nome de arquivo — no
        // Graph é identificador opaco, sem extensão —, e deduzir dali fazia todo anexo parecer PDF.
        builder.Property(e => e.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(CaptureItem.CONTENT_TYPE_MAX_LENGTH);

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(CaptureItem.FILE_NAME_MAX_LENGTH);

        builder.Property(e => e.Sender)
            .HasColumnName("sender")
            .HasMaxLength(CaptureItem.SENDER_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(CaptureItem.SUBJECT_MAX_LENGTH);

        builder.Property(e => e.ReceivedAt).HasColumnName("received_at").IsRequired();

        builder.Property(e => e.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(CaptureItem.CONTENT_HASH_MAX_LENGTH);

        builder.Property(e => e.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(CaptureItem.STORAGE_KEY_MAX_LENGTH);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<CaptureItemStatus>(id))
            .IsRequired();

        builder.Property(e => e.Routing)
            .HasColumnName("routing_confidence")
            .HasConversion(r => r!.Id, id => Enumeration.FromValue<RoutingConfidence>(id));

        builder.Property(e => e.SourceUrl)
            .HasColumnName("source_url")
            .HasMaxLength(CaptureItem.SOURCE_URL_MAX_LENGTH);

        // Derivado do SourceUrl, como HasStoredArtifact é do StorageKey: sem isto o EF cria uma
        // coluna `link_host` que ninguém escreve e que passaria a divergir da URL na primeira
        // atualização.
        builder.Ignore(e => e.LinkHost);

        // QUAL campo do PayerProfile derivou a senha do PDF — jamais a senha (ADR-009).
        builder.Property(e => e.UnlockedBy)
            .HasColumnName("unlocked_by")
            .HasMaxLength(CaptureItem.UNLOCKED_BY_MAX_LENGTH);

        builder.Property(e => e.Extraction)
            .HasColumnName("extraction_method")
            .HasConversion(m => m!.Id, id => Enumeration.FromValue<ExtractionMethod>(id));

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasMaxLength(CaptureItem.REASON_MAX_LENGTH);

        builder.Property(e => e.BillId)
            .HasColumnName("bill_id")
            .HasConversion(id => id!.Value.Value, value => BillId.From(value));

        builder.Property(e => e.DiscardedOf)
            .HasColumnName("discarded_of")
            .HasConversion(id => id!.Value.Value, value => CaptureItemId.From(value));

        builder.Property(e => e.ClaimedBy)
            .HasColumnName("claimed_by")
            .HasConversion(id => id!.Value.Value, value => UserId.From(value));

        builder.Property(e => e.ClaimedAt).HasColumnName("claimed_at");

        // Reprovação: decisão humana, com autor, no mesmo molde da reivindicação. Sem o autor a
        // fila de quarentena deixaria de ser auditável — e reprovar é a única operação dela que
        // tira trabalho da vista sem ninguém ter conferido o documento.
        builder.Property(e => e.DismissedBy)
            .HasColumnName("dismissed_by")
            .HasConversion(id => id!.Value.Value, value => UserId.From(value));

        builder.Property(e => e.DismissedAt).HasColumnName("dismissed_at");

        // Anexo manual: muda o caminho do processamento (lê do balde, não do provedor) e a
        // retenção (arquivo escolhido por uma pessoa nunca é purgado por desfecho).
        builder.Property(e => e.ManuallySupplied)
            .HasColumnName("manually_supplied")
            .HasDefaultValue(false)
            .IsRequired();

        // Tentativas e aluguel: o que impede o laço eterno medido em 2026-08-26 (1.709 tentativas
        // do mesmo erro em 4 itens). O contador cresce no claim, não no fim, para que a falha que
        // derruba o worker antes de escrever também consuma orçamento.
        builder.Property(e => e.ProcessingAttempts)
            .HasColumnName("processing_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(CaptureItem.LAST_ERROR_MAX_LENGTH);

        builder.Property(e => e.LeaseExpiresAt).HasColumnName("lease_expires_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Idempotência da ingestão. A chave inclui artifact_key porque um e-mail com três
        // boletos gera três itens — sem ela, dois seriam descartados como se fossem a mesma coisa.
        // Reprocessar a caixa não pode criar item novo; a mesma mensagem lida por DUAS fontes de
        // dois tenants gera dois itens, e isso é correto (ADR-008), garantido pelo tenant_id na chave.
        builder.HasIndex(e => new { e.TenantId, e.SourceId, e.ExternalMessageId, e.ArtifactKey })
            .IsUnique()
            .HasDatabaseName("ix_capture_items_tenant_source_message_artifact");

        // Dedup por conteúdo: o mesmo boleto reenviado noutra thread tem outro external_message_id.
        builder.HasIndex(e => new { e.TenantId, e.ContentHash })
            .HasDatabaseName("ix_capture_items_tenant_content_hash");

        // Fila de quarentena — filtrada por status dentro do tenant.
        builder.HasIndex(e => new { e.TenantId, e.Status, e.ReceivedAt })
            .HasDatabaseName("ix_capture_items_tenant_status_received");

        // Fila dos workers, e por isso SEM tenant_id na chave — ao contrário do índice acima,
        // que serve à tela. O worker roda fora de requisição e varre a instalação inteira; com
        // tenant_id na frente, o `ORDER BY received_at` do claim não seria servido por índice
        // nenhum. A ordem das colunas segue a do claim: status filtra, received_at ordena.
        builder.HasIndex(e => new { e.Status, e.ReceivedAt, e.Id })
            .HasDatabaseName("ix_capture_items_worker_queue");
    }
}
