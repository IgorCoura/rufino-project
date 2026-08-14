namespace TenantManagement.Infra.Persistence;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

public sealed class TenantManagementDbContext : DbContext, IUnitOfWork
{
    public const string DEFAULT_SCHEMA = "tenant_management";

    private readonly IDomainEventDispatcher? _dispatcher;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ClientRequest> ClientRequests => Set<ClientRequest>();

    public TenantManagementDbContext(
        DbContextOptions<TenantManagementDbContext> options,
        IDomainEventDispatcher? dispatcher = null) : base(options)
    {
        _dispatcher = dispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantManagementDbContext).Assembly);
    }

    /// <summary>
    /// Persiste e, <strong>depois do commit</strong>, entrega os eventos aos handlers.
    /// </summary>
    /// <remarks>
    /// A ordem é deliberada. O consumidor destes eventos fala com o provedor de identidade,
    /// que não participa da transação do banco: despachar antes do commit poderia conceder
    /// acesso a um tenant que a transação seguinte desfaz. Depois do commit, o pior caso é
    /// um acesso que ficou pendente — estado que o agregado registra e o endpoint de
    /// reprovisionamento conserta.
    /// <para>
    /// Não há outbox aqui, e a diferença importa: se o processo morrer entre o commit e o
    /// despacho, o vínculo fica <c>Pending</c> para sempre até alguém reprovisionar. É o
    /// preço aceito por um cadastro operado por gente, com volume baixo; se o BC passar a
    /// registrar tenant por conta própria, o outbox do BillPayment é o próximo passo.
    /// </para>
    /// </remarks>
    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var events = DrainDomainEvents();

        var affected = await base.SaveChangesAsync(cancellationToken);

        if (_dispatcher is not null)
        {
            foreach (var domainEvent in events)
                await _dispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        return affected;
    }

    /// <remarks>
    /// Um <c>case</c> por Aggregate Root porque <c>AggregateRoot&lt;TId&gt;</c> é genérico e não
    /// tem base não-genérica comum — <strong>acrescente o novo agregado aqui ao criá-lo</strong>,
    /// senão ele emite eventos que ninguém entrega e a falha é silenciosa.
    /// </remarks>
    private List<IDomainEvent> DrainDomainEvents()
    {
        var events = new List<IDomainEvent>();

        foreach (var entry in ChangeTracker.Entries())
        {
            var drained = entry.Entity switch
            {
                AggregateRoot<TenantId> aggregate => aggregate.PullDomainEvents(),
                _ => null,
            };

            if (drained is { Count: > 0 })
                events.AddRange(drained);
        }

        return events;
    }
}
