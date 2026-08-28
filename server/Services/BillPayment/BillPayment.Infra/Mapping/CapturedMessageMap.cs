namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CapturedMessageMap : IEntityTypeConfiguration<CapturedMessage>
{
    public void Configure(EntityTypeBuilder<CapturedMessage> builder)
    {
        builder.ToTable("captured_messages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CapturedMessageId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        // Referência a outro Aggregate: só o id, sem navigation property e sem FK.
        builder.Property(e => e.SourceId)
            .HasColumnName("source_id")
            .HasConversion(id => id.Value, value => CaptureSourceId.From(value))
            .IsRequired();

        builder.Property(e => e.ExternalMessageId)
            .HasColumnName("external_message_id")
            .HasMaxLength(CapturedMessage.EXTERNAL_MESSAGE_ID_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.InternetMessageId)
            .HasColumnName("internet_message_id")
            .HasMaxLength(CapturedMessage.INTERNET_MESSAGE_ID_MAX_LENGTH);

        builder.Property(e => e.Sender)
            .HasColumnName("sender")
            .HasMaxLength(CapturedMessage.SENDER_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(CapturedMessage.SUBJECT_MAX_LENGTH);

        builder.Property(e => e.BodyStorageKey)
            .HasColumnName("body_storage_key")
            .HasMaxLength(CapturedMessage.BODY_STORAGE_KEY_MAX_LENGTH);

        builder.Property(e => e.BodyContentType)
            .HasColumnName("body_content_type")
            .HasMaxLength(CapturedMessage.BODY_CONTENT_TYPE_MAX_LENGTH);

        builder.Property(e => e.ReceivedAt).HasColumnName("received_at").IsRequired();
        builder.Property(e => e.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.OwnsMany(e => e.Artifacts, ConfigureArtifacts);

        // Um registro por mensagem — a chave NÃO inclui o artefato, ao contrário da do
        // capture_items: lá o agregado é o anexo, aqui é a mensagem inteira.
        builder.HasIndex(
                nameof(CapturedMessage.TenantId),
                nameof(CapturedMessage.SourceId),
                nameof(CapturedMessage.ExternalMessageId))
            .IsUnique()
            .HasDatabaseName("ix_captured_messages_tenant_source_message");

        // A listagem é sempre por tenant, do mais recente para o mais antigo, e o cursor
        // desempata por Id na MESMA direção — cruzar as direções faz ORDER BY e WHERE
        // discordarem sobre quem já foi visto.
        builder.HasIndex(nameof(CapturedMessage.TenantId), nameof(CapturedMessage.ReceivedAt), nameof(CapturedMessage.Id))
            .IsDescending(false, true, true)
            .HasDatabaseName("ix_captured_messages_tenant_received");
    }

    private static void ConfigureArtifacts(OwnedNavigationBuilder<CapturedMessage, MessageArtifact> artifacts)
    {
        artifacts.ToTable("captured_message_artifacts");

        // A FK sombra tem que ter o MESMO tipo da chave da raiz — CapturedMessageId, não Guid.
        // Declarada como Guid, o EF recusa o modelo inteiro na validação.
        artifacts.Property<CapturedMessageId>("captured_message_id")
            .HasColumnName("captured_message_id")
            .HasConversion(id => id.Value, value => CapturedMessageId.From(value));

        artifacts.WithOwner().HasForeignKey("captured_message_id");

        artifacts.HasKey(a => a.Id);
        artifacts.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => MessageArtifactId.From(value))
            .ValueGeneratedNever();

        artifacts.Property(a => a.ArtifactKey)
            .HasColumnName("artifact_key")
            .HasMaxLength(MessageArtifact.ARTIFACT_KEY_MAX_LENGTH)
            .IsRequired();

        artifacts.Property(a => a.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(MessageArtifact.FILE_NAME_MAX_LENGTH);

        artifacts.Property(a => a.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(MessageArtifact.CONTENT_TYPE_MAX_LENGTH);

        artifacts.Property(a => a.Outcome)
            .HasColumnName("outcome")
            .HasConversion(o => o.Id, value => Enumeration.FromValue<ArtifactOutcome>(value))
            .IsRequired();

        artifacts.Property(a => a.Reason)
            .HasColumnName("reason")
            .HasMaxLength(MessageArtifact.REASON_MAX_LENGTH);

        artifacts.Property(a => a.CaptureItemId)
            .HasColumnName("capture_item_id")
            .HasConversion(
                id => id!.Value.Value,
                value => CaptureItemId.From(value));

        artifacts.Property(a => a.BillId)
            .HasColumnName("bill_id")
            .HasConversion(
                id => id!.Value.Value,
                value => BillId.From(value));

        artifacts.Property(a => a.DecidedAt).HasColumnName("decided_at");
        artifacts.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        artifacts.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();

        artifacts.HasIndex("captured_message_id", nameof(MessageArtifact.ArtifactKey))
            .IsUnique()
            .HasDatabaseName("ix_captured_message_artifacts_message_key");
    }
}
