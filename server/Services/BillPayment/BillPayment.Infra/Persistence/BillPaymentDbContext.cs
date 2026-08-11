namespace BillPayment.Infra.Persistence;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.EntityFrameworkCore;

public sealed class BillPaymentDbContext : DbContext, IUnitOfWork
{
    public const string DEFAULT_SCHEMA = "bill_payment";

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<TrustedOrigin> TrustedOrigins => Set<TrustedOrigin>();
    public DbSet<Payee> Payees => Set<Payee>();
    public DbSet<PayerProfile> PayerProfiles => Set<PayerProfile>();
    public DbSet<CaptureSource> CaptureSources => Set<CaptureSource>();
    public DbSet<CaptureItem> CaptureItems => Set<CaptureItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxDeadLetter> OutboxDeadLetters => Set<OutboxDeadLetter>();
    public DbSet<ProcessedEventLog> ProcessedEventLogs => Set<ProcessedEventLog>();
    public DbSet<ClientRequest> ClientRequests => Set<ClientRequest>();

    /// <summary>
    /// Credenciais de tenant cifradas. Infraestrutura, sem Aggregate — escrita e leitura só
    /// pelo <c>ISecretVault</c>, nunca por repositório nem por query.
    /// </summary>
    public DbSet<TenantSecret> TenantSecrets => Set<TenantSecret>();

    public BillPaymentDbContext(DbContextOptions<BillPaymentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillPaymentDbContext).Assembly);

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
        DrainDomainEvents();

        // Mensagens de outbox e efeito entram na MESMA transação implícita do SaveChanges.
        // É isso que torna a publicação atômica com a mutação: ou os dois existem, ou nenhum.
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Move os eventos acumulados nos agregados rastreados para <c>outbox_messages</c>.
    /// </summary>
    /// <remarks>
    /// Um <c>case</c> por Aggregate Root porque <c>AggregateRoot&lt;TId&gt;</c> é genérico e não
    /// tem base não-genérica comum — **acrescente o novo agregado aqui ao criá-lo**, senão
    /// ele emite eventos que ninguém publica e a falha é silenciosa.
    /// <para>
    /// O drain acontece depois do <c>DetectChanges</c> e antes do <c>SaveChanges</c>: os
    /// eventos precisam já estar no ChangeTracker para serem gravados na mesma transação.
    /// </para>
    /// </remarks>
    private void DrainDomainEvents()
    {
        var events = new List<IDomainEvent>();

        foreach (var entry in ChangeTracker.Entries())
        {
            var drained = entry.Entity switch
            {
                AggregateRoot<BillId> aggregate => aggregate.PullDomainEvents(),
                _ => null,
            };

            if (drained is { Count: > 0 })
                events.AddRange(drained);
        }

        foreach (var domainEvent in events)
            OutboxMessages.Add(OutboxMessage.From(domainEvent));
    }
}
