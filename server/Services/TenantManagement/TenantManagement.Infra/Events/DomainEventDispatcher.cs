namespace TenantManagement.Infra.Events;

using TenantManagement.Domain.SeedWork;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var invoker = (IHandlerInvoker)Activator.CreateInstance(
            typeof(HandlerInvoker<>).MakeGenericType(eventType))!;

        foreach (var handler in serviceProvider.GetServices(handlerInterface).Where(h => h is not null))
        {
            await invoker.HandleAsync(handler!, domainEvent, cancellationToken);
        }
    }

    // Fecha o genérico aberto IDomainEventHandler<> em tempo de execução para invocar o
    // HandleAsync tipado sem reflexão no caminho quente.
    private interface IHandlerInvoker
    {
        Task HandleAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TDomainEvent> : IHandlerInvoker where TDomainEvent : IDomainEvent
    {
        public Task HandleAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken)
            => ((IDomainEventHandler<TDomainEvent>)handler).HandleAsync((TDomainEvent)domainEvent, cancellationToken);
    }
}
