namespace EconomicCore.Infra.Mapping;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EconomicResourceMap : IEntityTypeConfiguration<EconomicResource>
{
    public void Configure(EntityTypeBuilder<EconomicResource> builder)
    {
        builder.ToTable("economic_resources");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EconomicResourceId.From(v))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(v => v.Value, v => TenantId.From(v))
            .IsRequired();

        builder.Property(e => e.TypeId)
            .HasColumnName("type_id")
            .HasConversion(v => v!.Value.Value, v => EconomicResourceTypeId.From(v));

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<ResourceKind>(v))
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(EconomicResource.NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(e => e.CustodianId)
            .HasColumnName("custodian_id")
            .HasConversion(v => v!.Value.Value, v => EconomicAgentId.From(v));

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_economic_resources_tenant_id");
    }
}
