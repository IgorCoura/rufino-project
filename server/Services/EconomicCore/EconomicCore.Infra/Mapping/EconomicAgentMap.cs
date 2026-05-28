namespace EconomicCore.Infra.Mapping;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EconomicAgentMap : IEntityTypeConfiguration<EconomicAgent>
{
    public void Configure(EntityTypeBuilder<EconomicAgent> builder)
    {
        builder.ToTable("economic_agents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EconomicAgentId.From(v))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(v => v.Value, v => TenantId.From(v))
            .IsRequired();

        builder.Property(e => e.Scope)
            .HasColumnName("scope")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<AgentScope>(v))
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(EconomicAgent.NAME_MAX_LENGTH)
            .IsRequired();

        builder.OwnsOne(e => e.TaxId, tax =>
        {
            tax.Property(t => t.Value).HasColumnName("tax_id_value").HasMaxLength(14);
            tax.Property(t => t.Kind)
                .HasColumnName("tax_id_kind")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<TaxIdKind>(v));
        });

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.Roles);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_economic_agents_tenant_id");
    }
}
