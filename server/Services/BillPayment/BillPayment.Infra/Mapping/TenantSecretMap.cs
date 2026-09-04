namespace BillPayment.Infra.Mapping;

using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TenantSecretMap : IEntityTypeConfiguration<TenantSecret>
{
    public void Configure(EntityTypeBuilder<TenantSecret> builder)
    {
        builder.ToTable("tenant_secrets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").IsRequired();
        builder.Property(e => e.KekVersion).HasColumnName("kek_version").IsRequired();

        builder.Property(e => e.WrappedDek).HasColumnName("wrapped_dek").IsRequired();
        builder.Property(e => e.DekNonce).HasColumnName("dek_nonce").IsRequired();
        builder.Property(e => e.DekTag).HasColumnName("dek_tag").IsRequired();

        builder.Property(e => e.Ciphertext).HasColumnName("ciphertext").IsRequired();
        builder.Property(e => e.Nonce).HasColumnName("nonce").IsRequired();
        builder.Property(e => e.Tag).HasColumnName("tag").IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Buscar as credenciais de um tenant (revisão de acesso, remoção em cascata de conta)
        // é a única consulta que não é por chave primária.
        builder.HasIndex(e => new { e.TenantId, e.Kind }).HasDatabaseName("ix_tenant_secrets_tenant_kind");
    }
}
