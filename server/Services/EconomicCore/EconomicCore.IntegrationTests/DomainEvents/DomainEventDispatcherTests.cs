namespace EconomicCore.IntegrationTests.DomainEvents;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Infra;
using Microsoft.Extensions.DependencyInjection;

public sealed class DomainEventDispatcherTests
{
    private sealed record FakeDomainEvent(Guid EventId, DateTime OccurredAt) : IDomainEvent
    {
        public static FakeDomainEvent New() => new(Guid.NewGuid(), DateTime.UtcNow);
    }

    private sealed record OtherDomainEvent(Guid EventId, DateTime OccurredAt) : IDomainEvent
    {
        public static OtherDomainEvent New() => new(Guid.NewGuid(), DateTime.UtcNow);
    }

    private sealed class Probe
    {
        public List<IDomainEvent> Handled { get; } = [];
    }

    private sealed class FakeHandler(Probe probe) : IDomainEventHandler<FakeDomainEvent>
    {
        public Task HandleAsync(FakeDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            probe.Handled.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondFakeHandler(Probe probe) : IDomainEventHandler<FakeDomainEvent>
    {
        public Task HandleAsync(FakeDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            probe.Handled.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private static ServiceProvider BuildProvider(Probe probe, Action<IServiceCollection> registerHandlers)
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatcher();
        services.AddSingleton(probe);
        registerHandlers(services);
        return services.BuildServiceProvider();
    }

    // Despachar um evento com um handler registrado invoca o handler com a instância correta do evento.
    [Fact]
    public async Task DispatchAsync_WhenSingleHandlerRegistered_InvokesHandlerWithEvent()
    {
        var probe = new Probe();
        using var provider = BuildProvider(probe, s => s.AddScoped<IDomainEventHandler<FakeDomainEvent>, FakeHandler>());
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var evt = FakeDomainEvent.New();
        await dispatcher.DispatchAsync(evt);

        Assert.Single(probe.Handled);
        Assert.Same(evt, probe.Handled[0]);
    }

    // Vários handlers registrados para o mesmo evento são todos invocados.
    [Fact]
    public async Task DispatchAsync_WhenMultipleHandlersRegistered_InvokesAll()
    {
        var probe = new Probe();
        using var provider = BuildProvider(probe, s =>
        {
            s.AddScoped<IDomainEventHandler<FakeDomainEvent>, FakeHandler>();
            s.AddScoped<IDomainEventHandler<FakeDomainEvent>, SecondFakeHandler>();
        });
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(FakeDomainEvent.New());

        Assert.Equal(2, probe.Handled.Count);
    }

    // Despachar um evento sem handler registrado é no-op (não lança).
    [Fact]
    public async Task DispatchAsync_WhenNoHandlerRegistered_DoesNotThrow()
    {
        var probe = new Probe();
        using var provider = BuildProvider(probe, _ => { });
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(OtherDomainEvent.New());

        Assert.Empty(probe.Handled);
    }

    // Despacho resolve handlers pelo tipo concreto do evento — handler de outro tipo não é chamado.
    [Fact]
    public async Task DispatchAsync_ResolvesHandlersByConcreteEventType()
    {
        var probe = new Probe();
        using var provider = BuildProvider(probe, s => s.AddScoped<IDomainEventHandler<FakeDomainEvent>, FakeHandler>());
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(OtherDomainEvent.New());

        Assert.Empty(probe.Handled);
    }
}
