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

        // Tabela filha, e nao coluna jsonb: a pasta tem ciclo de vida proprio (cursor que avanca,
        // erro que aparece e some) e o EF rastreia bem coleção owned de escalares — é o mesmo
        // critério que colocou bill_checks em tabela e os retratos de consulta em jsonb.
        // Coleção owned: carrega junto com a raiz sem Include, e não existe fora dela — que é
        // exatamente a fronteira do Aggregate. Não há repositório de pasta, por desenho.
        builder.OwnsMany(e => e.Folders, ConfigureFolders);

        // Anulável porque nulo é "sem limite" — o comportamento de sempre, e o que toda fonte
        // conectada antes deste campo continua tendo. `date` e não `timestamp`: quem escolhe é
        // uma pessoa num calendário, e a conversão para instante é do adapter.
        builder.Property(e => e.CaptureSince)
            .HasColumnName("capture_since")
            .HasColumnType("date");

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

    /// <summary>
    /// As pastas acompanhadas, cada uma com o próprio cursor.
    /// </summary>
    /// <remarks>
    /// <strong>O caminho nulo é a caixa de entrada, e o índice único precisa lidar com isso.</strong>
    /// No Postgres, <c>NULL</c> não colide com <c>NULL</c> em índice único comum, então duas
    /// linhas de caixa de entrada passariam pelo banco — quem recusa é o agregado
    /// (<c>BLP.CPS16</c>), e o índice usa <c>NULLS NOT DISTINCT</c> para o banco recusar também
    /// sob concorrência.
    /// </remarks>
    private static void ConfigureFolders(OwnedNavigationBuilder<CaptureSource, MonitoredFolder> folders)
    {
        folders.ToTable("capture_source_folders");

        // A FK sombra tem que ter o MESMO tipo da chave da raiz — CaptureSourceId, não Guid.
        // Declarada como Guid, o EF recusa o modelo inteiro na validação ("cannot target the
        // primary key ... because it is not compatible"), e a falha derruba o OnModelCreating,
        // levando junto toda a suíte de integração.
        folders.Property<CaptureSourceId>("capture_source_id")
            .HasColumnName("capture_source_id")
            .HasConversion(id => id.Value, value => CaptureSourceId.From(value));

        folders.WithOwner().HasForeignKey("capture_source_id");

        folders.HasKey(f => f.Id);
        folders.Property(f => f.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => MonitoredFolderId.From(value))
            .ValueGeneratedNever();

        // Nulo = caixa de entrada. Nulo em vez do nome traduzido porque "Caixa de Entrada" muda
        // com o idioma da conta, e o adapter resolve o nome bem-conhecido do provedor.
        folders.Property(f => f.Path)
            .HasColumnName("path")
            .HasMaxLength(MonitoredFolder.PATH_MAX_LENGTH);

        folders.Property(f => f.SyncCursor)
            .HasColumnName("sync_cursor")
            .HasMaxLength(MonitoredFolder.SYNC_CURSOR_MAX_LENGTH);

        folders.Property(f => f.LastSyncAt).HasColumnName("last_sync_at");

        folders.Property(f => f.LastSyncError)
            .HasColumnName("last_sync_error")
            .HasMaxLength(MonitoredFolder.SYNC_ERROR_MAX_LENGTH);

        folders.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();
        folders.Property(f => f.UpdatedAt).HasColumnName("updated_at").IsRequired();

        folders.HasIndex("capture_source_id", nameof(MonitoredFolder.Path))
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("ix_capture_source_folders_source_path");
    }
}
