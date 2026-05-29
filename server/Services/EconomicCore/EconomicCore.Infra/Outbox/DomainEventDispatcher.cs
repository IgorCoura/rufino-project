namespace EconomicCore.Infra.Outbox;

using EconomicCore.Domain.SeedWork;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var invoker = (HandlerInvoker)Activator.CreateInstance(
            typeof(HandlerInvoker<>).MakeGenericType(eventType))!;

        foreach (var handler in serviceProvider.GetServices(handlerInterface))
        {
            if (handler is not null)
                await invoker.HandleAsync(handler, domainEvent, cancellationToken);
        }
    }

    // Closes the open generic IDomainEventHandler<> at runtime so we can invoke the strongly-typed
    // HandleAsync without reflection on the hot path (the event's concrete type is only known after deserialization).
    private abstract class HandlerInvoker
    {
        public abstract Task HandleAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TDomainEvent> : HandlerInvoker where TDomainEvent : IDomainEvent
    {
        public override Task HandleAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken)
            => ((IDomainEventHandler<TDomainEvent>)handler).HandleAsync((TDomainEvent)domainEvent, cancellationToken);
    }
}
