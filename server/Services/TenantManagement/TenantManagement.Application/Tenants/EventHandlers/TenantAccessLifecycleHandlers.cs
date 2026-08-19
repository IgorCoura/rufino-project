namespace TenantManagement.Application.Tenants.EventHandlers;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Suspender um tenant corta o acesso de <strong>todo mundo</strong>, o titular incluído.
/// </summary>
/// <remarks>
/// <c>MembershipRole.Owner</c> distingue quem responde pela conta, não quem entra: suspender por
/// inadimplência e deixar o dono aprovando pagamento seria suspender nada. Até 2026-08-17 este
/// handler não existia — o evento era emitido e ninguém escutava, então um tenant suspenso
/// seguia operando os dois produtos, contra o que o próprio <c>TenantStatus.Suspended</c> declara.
/// </remarks>
public sealed class TenantSuspendedDomainEventHandler(TenantAccessSynchronizer synchronizer)
    : IDomainEventHandler<TenantSuspendedDomainEvent>
{
    public Task HandleAsync(TenantSuspendedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return synchronizer.SyncAsync(domainEvent.TenantId, cancellationToken);
    }
}

/// <summary>Reativar devolve o acesso aos vínculos ativos, nos produtos que o tenant tem hoje.</summary>
/// <remarks>
/// Os produtos vêm do agregado, não de um retrato de antes da suspensão: se um produto foi
/// cancelado enquanto o tenant estava suspenso, reativar não pode ressuscitá-lo.
/// </remarks>
public sealed class TenantReactivatedDomainEventHandler(TenantAccessSynchronizer synchronizer)
    : IDomainEventHandler<TenantReactivatedDomainEvent>
{
    public Task HandleAsync(TenantReactivatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return synchronizer.SyncAsync(domainEvent.TenantId, cancellationToken);
    }
}

/// <summary>Habilitar um produto passa a valer no token de quem já tem acesso ao tenant.</summary>
public sealed class ProductActivatedDomainEventHandler(TenantAccessSynchronizer synchronizer)
    : IDomainEventHandler<ProductActivatedDomainEvent>
{
    public Task HandleAsync(ProductActivatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return synchronizer.SyncAsync(domainEvent.TenantId, cancellationToken);
    }
}

/// <summary>
/// Desabilitar um produto tira o tenant do escopo daquele produto — e só dele.
/// </summary>
/// <remarks>
/// O acesso aos demais produtos continua. É por isso que a porta recebe o conjunto de produtos
/// ativos e declara o estado desejado, em vez de ter um método "desativar produto": o provedor
/// não precisa saber qual das mudanças aconteceu, só qual é o resultado.
/// </remarks>
public sealed class ProductDeactivatedDomainEventHandler(TenantAccessSynchronizer synchronizer)
    : IDomainEventHandler<ProductDeactivatedDomainEvent>
{
    public Task HandleAsync(ProductDeactivatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return synchronizer.SyncAsync(domainEvent.TenantId, cancellationToken);
    }
}
