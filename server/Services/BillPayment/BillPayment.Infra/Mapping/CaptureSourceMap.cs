namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CaptureSourceMap : IEntityTypeConfiguration<CaptureSource>
{
    public void Configure(EntityTypeBuilder<CaptureSource> builder)
    {
        builder.ToTable("capture_sources");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CaptureSourceId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.Id, id => Enumeration.FromValue<CaptureSourceKind>(id))
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(CaptureSource.DISPLAY_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.Address)
            .HasColumnName("address")
            .HasMaxLength(CaptureSource.ADDRESS_MAX_LENGTH)
            .IsRequired();

        // Ponteiro para o cofre, nunca o segredo. Nulo é legítimo: ManualUpload não guarda
        // credencial (CaptureSourceKind.RequiresCredential), e quem recusa o nulo indevido é
        // o agregado, com BLP.CPS01.
        builder.Property(e => e.Credential)
            .HasColumnName("credential_ref")
            .HasMaxLength(CredentialRefConversions.MAX_LENGTH)
            .HasConversion(CredentialRefConversions.Single, CredentialRefConversions.SingleComparer);

        // Nulo = caixa de entrada inteira. Minimizacao de dado na origem: o que nao e lido
        // nao precisa ser protegido, cifrado nem apagado depois.
        builder.Property(e => e.FolderPath)
            .HasColumnName("folder_path")
            .HasMaxLength(CaptureSource.FOLDER_PATH_MAX_LENGTH);

        builder.Property(e => e.SyncCursor)
            .HasColumnName("sync_cursor")
            .HasMaxLength(CaptureSource.SYNC_CURSOR_MAX_LENGTH);

        builder.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");

        builder.Property(e => e.LastSyncError)
            .HasColumnName("last_sync_error")
            .HasMaxLength(CaptureSource.SYNC_ERROR_MAX_LENGTH);

        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Invariante BLP.CPS10, dentro do tenant. O banco é quem garante sob concorrência.
        builder.HasIndex(e => new { e.TenantId, e.Address })
            .IsUnique()
            .HasDatabaseName("ix_capture_sources_tenant_address");

        // ATENÇÃO: índice sobre o endereço SEM tenant_id, e deliberadamente NÃO único.
        // Duas contas monitorando a mesma caixa é o caso previsto pelo ADR-008, não um erro —
        // tornar isto único quebraria a funcionalidade central de fonte compartilhada.
        // Ele serve a UM caminho de código, ICaptureSourceRepository.IsAddressMonitoredByAnyTenantAsync,
        // que devolve bool e nada mais. Qualquer outra consulta sem tenant_id sobre esta tabela
        // é violação do isolamento.
        builder.HasIndex(e => e.Address)
            .HasDatabaseName("ix_capture_sources_address_global");

        // Cobre a varredura do worker de sincronização, que busca as fontes habilitadas.
        builder.HasIndex(e => new { e.IsEnabled, e.LastSyncAt })
            .HasDatabaseName("ix_capture_sources_enabled_last_sync");
    }
}
