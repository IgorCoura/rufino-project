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

        builder.OwnsMany(e => e.Allocations, a =>
        {
            a.ToTable("economic_event_allocations");
            a.WithOwner().HasForeignKey("economic_event_id");
            a.Property<int>("id").ValueGeneratedOnAdd();
            a.HasKey("id");

            a.OwnsOne(x => x.Commitment, cr =>
            {
                cr.Property(c => c.ContractId)
                    .HasColumnName("covering_contract_id")
                    .HasConversion(v => v.Value, v => EconomicContractId.From(v))
                    .IsRequired();
                cr.Property(c => c.CommitmentId)
                    .HasColumnName("covering_commitment_id")
                    .HasConversion(v => v.Value, v => CommitmentId.From(v))
                    .IsRequired();
            });

            a.OwnsOne(x => x.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency)
                    .HasColumnName("amount_currency")
                    .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v))
                    .IsRequired();
            });
        });

        builder.OwnsMany(e => e.DualityLinks, dl =>
        {
            dl.ToTable("economic_event_duality_links");
            dl.WithOwner().HasForeignKey("economic_event_id");
            dl.Property<int>("id").ValueGeneratedOnAdd();
            dl.HasKey("id");

            dl.Property(d => d.CounterpartEventId)
                .HasColumnName("counterpart_event_id")
                .HasConversion(v => v.Value, v => EconomicEventId.From(v))
                .IsRequired();

            dl.Property(d => d.CommitmentId)
                .HasColumnName("commitment_id")
                .HasConversion(v => v!.Value.Value, v => CommitmentId.From(v));

            dl.Property(d => d.MatchedAmountValue)
                .HasColumnName("matched_amount").HasColumnType("decimal(18,2)").IsRequired();
            dl.Property(d => d.MatchedCurrency)
                .HasColumnName("matched_currency")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v))
                .IsRequired();
            dl.Ignore(d => d.MatchedAmount);
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
