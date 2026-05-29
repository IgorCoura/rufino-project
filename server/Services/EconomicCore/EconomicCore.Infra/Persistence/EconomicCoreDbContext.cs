namespace EconomicCore.Infra.Persistence;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

public sealed class EconomicCoreDbContext : DbContext, IUnitOfWork
{
    public const string DEFAULT_SCHEMA = "economic_core";

    public DbSet<EconomicEvent> EconomicEvents => Set<EconomicEvent>();
    public DbSet<EconomicResource> EconomicResources => Set<EconomicResource>();
    public DbSet<EconomicAgent> EconomicAgents => Set<EconomicAgent>();
    public DbSet<EconomicContract> EconomicContracts => Set<EconomicContract>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxDeadLetter> OutboxDeadLetters => Set<OutboxDeadLetter>();
    public DbSet<ProcessedEventLog> ProcessedEventLogs => Set<ProcessedEventLog>();
    public DbSet<ClientRequest> ClientRequests => Set<ClientRequest>();

    public EconomicCoreDbContext(DbContextOptions<EconomicCoreDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomicCoreDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var fk in entityType.GetForeignKeys().ToList())
            {
                if (fk.IsOwnership) continue;
                fk.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var fksToRemove = entityType.GetForeignKeys()
                .Where(fk => !fk.IsOwnership && fk.PrincipalEntityType != entityType.FindOwnership()?.PrincipalEntityType)
                .ToList();

            foreach (var fk in fksToRemove)
            {
                entityType.RemoveForeignKey(fk);
            }
        }
    }

    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var domainEvents = new List<IDomainEvent>();
        foreach (var entity in ChangeTracker.Entries().Select(e => e.Entity))
        {
            switch (entity)
            {
                case AggregateRoot<EconomicEventId> evtAgg:
                    domainEvents.AddRange(evtAgg.PullDomainEvents());
                    break;
                case AggregateRoot<EconomicResourceId> resAgg:
                    domainEvents.AddRange(resAgg.PullDomainEvents());
                    break;
                case AggregateRoot<EconomicAgentId> agtAgg:
                    domainEvents.AddRange(agtAgg.PullDomainEvents());
                    break;
                case AggregateRoot<EconomicContractId> ctrAgg:
                    domainEvents.AddRange(ctrAgg.PullDomainEvents());
                    break;
            }
        }

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(OutboxMessage.From(domainEvent));
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
