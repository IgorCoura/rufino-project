namespace TenantManagement.Infra.Mapping;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TenantMap : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(k => k.Id, id => Enumeration.FromValue<TenantKind>(id))
            .IsRequired();

        builder.Property(e => e.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(Tenant.LEGAL_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.TradeName)
            .HasColumnName("trade_name")
            .HasMaxLength(Tenant.TRADE_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.PrimaryTaxId)
            .HasColumnName("primary_tax_id")
            .HasMaxLength(TaxIdConversions.MAX_LENGTH)
            .HasConversion(TaxIdConversions.Single, TaxIdConversions.SingleComparer)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(s => s.Id, id => Enumeration.FromValue<TenantStatus>(id))
            .IsRequired();

        builder.Property(e => e.SuspensionReason)
            .HasColumnName("suspension_reason")
            .HasMaxLength(Tenant.SUSPENSION_REASON_MAX_LENGTH)
            .IsRequired();

        // Derivada dos vínculos. Sem coluna de propósito: um campo próprio seria uma segunda
        // versão da mesma informação, livre para divergir do que os vínculos dizem.
        builder.Ignore(e => e.AccessProvisioning);

        builder.OwnsOne(e => e.Contact, contact =>
        {
            contact.Property(c => c.Email)
                .HasColumnName("contact_email")
                .HasMaxLength(ContactInfo.MAX_LENGTH_EMAIL)
                .IsRequired();

            contact.Property(c => c.Phone)
                .HasColumnName("contact_phone")
                .HasMaxLength(ContactInfo.MAX_LENGTH_PHONE)
                .IsRequired();
        });
        builder.Navigation(e => e.Contact).IsRequired();

        builder.OwnsOne(e => e.Address, address =>
        {
            address.Property(a => a.ZipCode).HasColumnName("address_zip_code").HasMaxLength(Address.ZIP_CODE_LENGTH).IsRequired();
            address.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(Address.MAX_LENGTH_STREET).IsRequired();
            address.Property(a => a.Number).HasColumnName("address_number").HasMaxLength(Address.MAX_LENGTH_NUMBER).IsRequired();
            address.Property(a => a.Complement).HasColumnName("address_complement").HasMaxLength(Address.MAX_LENGTH_COMPLEMENT).IsRequired();
            address.Property(a => a.Neighborhood).HasColumnName("address_neighborhood").HasMaxLength(Address.MAX_LENGTH_NEIGHBORHOOD).IsRequired();
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(Address.MAX_LENGTH_CITY).IsRequired();
            address.Property(a => a.State).HasColumnName("address_state").HasMaxLength(Address.STATE_LENGTH).IsRequired();
            address.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(Address.MAX_LENGTH_COUNTRY).IsRequired();
        });
        builder.Navigation(e => e.Address).IsRequired();

        // Produtos e vínculos são owned collections, e não entidades independentes: só existem
        // dentro do tenant, só são alcançáveis pela raiz, e morrem com ela. Owned também é o que
        // dispensa Include — o EF as carrega junto do agregado, sempre.
        builder.OwnsMany(e => e.Products, product =>
        {
            product.ToTable("tenant_products");

            product.HasKey(p => p.Id);
            product.Property(p => p.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => TenantProductId.From(value))
                .ValueGeneratedNever();

            product.Property(p => p.TenantId)
                .HasColumnName("tenant_id")
                .HasConversion(id => id.Value, value => TenantId.From(value))
                .IsRequired();

            product.WithOwner().HasForeignKey(p => p.TenantId);

            product.Property(p => p.Code)
                .HasColumnName("product_code")
                .HasConversion(c => c.Id, id => Enumeration.FromValue<ProductCode>(id))
                .IsRequired();

            product.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
            product.Property(p => p.ActivatedAt).HasColumnName("activated_at").IsRequired();
            product.Property(p => p.DeactivatedAt).HasColumnName("deactivated_at");
            product.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
            product.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // Um produto aparece uma vez por tenant: reabilitar reaproveita a linha em vez de
            // acrescentar outra, e o índice é quem garante isso sob concorrência.
            product.HasIndex(p => new { p.TenantId, p.Code })
                .IsUnique()
                .HasDatabaseName("ix_tenant_products_tenant_code");
        });

        builder.OwnsMany(e => e.Memberships, membership =>
        {
            membership.ToTable("tenant_memberships");

            membership.HasKey(m => m.Id);
            membership.Property(m => m.Id)
                .HasColumnName("id")
                .HasConversion(id => id.Value, value => TenantMembershipId.From(value))
                .ValueGeneratedNever();

            membership.Property(m => m.TenantId)
                .HasColumnName("tenant_id")
                .HasConversion(id => id.Value, value => TenantId.From(value))
                .IsRequired();

            membership.WithOwner().HasForeignKey(m => m.TenantId);

            membership.Property(m => m.Email)
                .HasColumnName("email")
                .HasMaxLength(TenantMembership.MAX_LENGTH_EMAIL)
                .IsRequired();

            membership.Property(m => m.Role)
                .HasColumnName("role")
                .HasConversion(r => r.Id, id => Enumeration.FromValue<MembershipRole>(id))
                .IsRequired();

            membership.Property(m => m.IdentityUserId)
                .HasColumnName("identity_user_id")
                .HasConversion(id => id!.Value.Value, value => UserId.From(value));

            membership.Property(m => m.Provisioning)
                .HasColumnName("provisioning")
                .HasConversion(p => p.Id, id => Enumeration.FromValue<ProvisioningStatus>(id))
                .IsRequired();

            membership.Property(m => m.IsActive).HasColumnName("is_active").IsRequired();
            membership.Property(m => m.RevokedAt).HasColumnName("revoked_at");
            membership.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
            membership.Property(m => m.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // O e-mail é a chave natural do vínculo dentro do tenant. Revogar mantém a linha,
            // então o índice cobre ativo e revogado: reconceder reaproveita, nunca duplica.
            membership.HasIndex(m => new { m.TenantId, m.Email })
                .IsUnique()
                .HasDatabaseName("ix_tenant_memberships_tenant_email");
        });

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Um documento fiscal, um tenant — TNM.TNT10. A checagem no handler evita o round-trip
        // no caso comum; quem resolve a corrida entre dois cadastros simultâneos é este índice.
        builder.HasIndex(e => e.PrimaryTaxId)
            .IsUnique()
            .HasDatabaseName("ix_tenants_primary_tax_id");

        // A listagem é keyset por (CreatedAt, Id) descendente — sem este índice, toda página
        // vira varredura completa assim que a base crescer.
        builder.HasIndex(e => new { e.CreatedAt, e.Id })
            .HasDatabaseName("ix_tenants_created_at_id");
    }
}
