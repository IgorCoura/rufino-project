namespace EconomicCore.Infra.Mapping;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EconomicEventMap : IEntityTypeConfiguration<EconomicEvent>
{
    public void Configure(EntityTypeBuilder<EconomicEvent> builder)
    {
        builder.ToTable("economic_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EconomicEventId.From(v))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(v => v.Value, v => TenantId.From(v))
            .IsRequired();

        builder.Property(e => e.Direction)
            .HasColumnName("direction")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<FlowDirection>(v))
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .HasColumnName("resource_id")
            .HasConversion(v => v.Value, v => EconomicResourceId.From(v))
            .IsRequired();

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("amount_currency")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v))
                .IsRequired();
        });

        builder.OwnsOne(e => e.OccurredAt, ts =>
        {
            ts.Property(t => t.InstantUtc).HasColumnName("occurred_at").IsRequired();
        });

        builder.Property(e => e.TypeId)
            .HasColumnName("type_id")
            .HasConversion(v => v!.Value.Value, v => EconomicEventTypeId.From(v));

        builder.OwnsMany(e => e.Participations, p =>
        {
            p.ToTable("economic_event_participations");
            p.WithOwner().HasForeignKey("economic_event_id");
            p.Property<int>("id").ValueGeneratedOnAdd();
            p.HasKey("id");

            p.Property(x => x.AgentId)
                .HasColumnName("agent_id")
                .HasConversion(v => v.Value, v => EconomicAgentId.From(v))
                .IsRequired();

            p.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<ParticipationRole>(v))
                .IsRequired();
        });

        builder.OwnsOne(e => e.Duality, dl =>
        {
            dl.Property(d => d.CounterpartEventId)
                .HasColumnName("duality_counterpart_event_id")
                .HasConversion(v => v.Value, v => EconomicEventId.From(v));

            dl.OwnsOne(d => d.MatchedAmount, ma =>
            {
                ma.Property(m => m.Amount).HasColumnName("duality_matched_amount").HasColumnType("decimal(18,2)");
                ma.Property(m => m.Currency)
                    .HasColumnName("duality_matched_currency")
                    .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v));
            });
        });

        builder.OwnsOne(e => e.CoveringCommitment, cr =>
        {
            cr.Property(c => c.ContractId)
                .HasColumnName("covering_contract_id")
                .HasConversion(v => v.Value, v => EconomicContractId.From(v));

            cr.Property(c => c.CommitmentId)
                .HasColumnName("covering_commitment_id")
                .HasConversion(v => v.Value, v => CommitmentId.From(v));
        });

        builder.OwnsOne(e => e.Competence, cp =>
        {
            cp.Property(c => c.Year).HasColumnName("competence_year").IsRequired();
            cp.Property(c => c.Month).HasColumnName("competence_month").IsRequired();
        });

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasConversion(v => v!.Value.Value, v => UserId.From(v));

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_economic_events_tenant_id");
        builder.HasIndex(e => new { e.TenantId, e.ResourceId }).HasDatabaseName("ix_economic_events_tenant_resource");
    }
}
