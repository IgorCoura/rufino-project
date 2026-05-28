namespace EconomicCore.Infra.Mapping;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Entities;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EconomicContractMap : IEntityTypeConfiguration<EconomicContract>
{
    public void Configure(EntityTypeBuilder<EconomicContract> builder)
    {
        builder.ToTable("economic_contracts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EconomicContractId.From(v))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(v => v.Value, v => TenantId.From(v))
            .IsRequired();

        builder.Property(e => e.CounterpartyId)
            .HasColumnName("counterparty_id")
            .HasConversion(v => v.Value, v => EconomicAgentId.From(v))
            .IsRequired();

        builder.Property(e => e.Direction)
            .HasColumnName("direction")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<ContractDirection>(v))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<ContractStatus>(v))
            .IsRequired();

        builder.OwnsOne(e => e.Recurrence, r =>
        {
            r.Property(x => x.Periodicity)
                .HasColumnName("recurrence_periodicity")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<Periodicity>(v))
                .IsRequired();
            r.Property(x => x.AnchorDay).HasColumnName("recurrence_anchor_day").IsRequired();
        });

        builder.OwnsOne(e => e.DefaultTerms, dt =>
        {
            dt.OwnsOne(x => x.ExpectedAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("expected_amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency)
                    .HasColumnName("expected_amount_currency")
                    .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v))
                    .IsRequired();
            });
            dt.Property(x => x.TolerancePercent).HasColumnName("tolerance_percent").HasColumnType("decimal(5,4)").IsRequired();
            dt.Property(x => x.WindowDays).HasColumnName("window_days").IsRequired();
        });

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Ignore(e => e.DomainEvents);

        var commitmentNav = builder.OwnsMany(e => e.Commitments, ConfigureCommitment);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_economic_contracts_tenant_id");
    }

    private static void ConfigureCommitment(OwnedNavigationBuilder<EconomicContract, Commitment> cb)
    {
        cb.ToTable("commitments");

        cb.HasKey(c => c.Id);
        cb.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => CommitmentId.From(v))
            .ValueGeneratedNever();

        cb.WithOwner().HasForeignKey("contract_id");

        cb.Property(c => c.Direction)
            .HasColumnName("direction")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<CommitmentDirection>(v))
            .IsRequired();

        cb.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion(v => v.Id, v => Enumeration.FromValue<CommitmentStatus>(v))
            .IsRequired();

        cb.OwnsOne(c => c.Period, p =>
        {
            p.Property(x => x.Year).HasColumnName("period_year").IsRequired();
            p.Property(x => x.Month).HasColumnName("period_month").IsRequired();
        });

        cb.OwnsOne(c => c.ExpectedAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("expected_amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("expected_amount_currency")
                .HasConversion(v => v.Id, v => Enumeration.FromValue<Currency>(v))
                .IsRequired();
        });

        cb.OwnsOne(c => c.FulfillmentWindow, fw =>
        {
            fw.Property(x => x.From).HasColumnName("fulfillment_from").IsRequired();
            fw.Property(x => x.To).HasColumnName("fulfillment_to").IsRequired();
        });

        cb.OwnsOne(c => c.Reciprocal, rl =>
        {
            rl.Property(x => x.ReciprocalCommitmentId)
                .HasColumnName("reciprocal_commitment_id")
                .HasConversion(v => v.Value, v => CommitmentId.From(v));
        });

        cb.Property(c => c.FulfillingEventId)
            .HasColumnName("fulfilling_event_id")
            .HasConversion(v => v!.Value.Value, v => EconomicEventId.From(v));

        cb.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        cb.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();

        cb.HasIndex("contract_id", nameof(Commitment.Direction))
            .HasDatabaseName("ix_commitments_contract_direction");
    }
}
