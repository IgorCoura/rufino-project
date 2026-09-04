namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PayerProfileMap : IEntityTypeConfiguration<PayerProfile>
{
    public void Configure(EntityTypeBuilder<PayerProfile> builder)
    {
        builder.ToTable("payer_profiles");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PayerProfileId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.Id, id => Enumeration.FromValue<PayerKind>(id))
            .IsRequired();

        builder.Property(e => e.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(PayerProfile.LEGAL_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.PrimaryTaxId)
            .HasColumnName("primary_tax_id")
            .HasMaxLength(TaxIdConversions.MAX_LENGTH)
            .HasConversion(TaxIdConversions.Single, TaxIdConversions.SingleComparer)
            .IsRequired();

        // Filiais e documentos correlatos em jsonb, não em tabela filha: a lista é lida
        // sempre junto com o cadastro e a pergunta que o roteamento faz — "este documento é
        // deste tenant?" — é respondida pelo agregado em memória, nunca por SQL.
        builder.Property(e => e.AdditionalTaxIds)
            .HasColumnName("additional_tax_ids")
            .HasColumnType("jsonb")
            .HasConversion(TaxIdConversions.Collection, TaxIdConversions.CollectionComparer)
            .IsRequired();

        builder.Property(e => e.MatchByCnpjRoot).HasColumnName("match_by_cnpj_root").IsRequired();

        // O ponteiro da subconta é um CredentialRef desde 2026-08-31 — mesma coluna de texto
        // esquema:chave do CaptureSource.Credential. Linha anterior à troca é nula (nada
        // produzia o valor), então o aperto de 200 para o tamanho do VO não corta dado.
        builder.Property(e => e.AsaasAccountRef)
            .HasColumnName("asaas_account_ref")
            .HasMaxLength(CredentialRefConversions.MAX_LENGTH)
            .HasConversion(CredentialRefConversions.Single, CredentialRefConversions.SingleComparer);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Um cadastro fiscal por tenant — BLP.PRF03. A checagem no handler evita o
        // round-trip no caso comum; quem resolve a corrida é este índice.
        builder.HasIndex(e => e.TenantId)
            .IsUnique()
            .HasDatabaseName("ix_payer_profiles_tenant");
    }
}
