namespace TenantManagement.Application;

using TenantManagement.Application.Behaviors;
using TenantManagement.Application.Mediator;
using TenantManagement.Application.Queries.Tenants;
using TenantManagement.Application.Tenants.EventHandlers;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationDependencies
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Mediator próprio (sem MediatR) — escaneia handlers e behaviors do assembly da Application.
        services.AddCustomMediator(typeof(ApplicationDependencies).Assembly);

        // LoggingBehavior é o behavior mais externo e o único ativo. Não há command multi-aggregate
        // neste BC — cada handler faz exatamente um SaveEntitiesAsync, e a transação implícita do
        // EF cobre tudo. IRequestManager (idempotência) é registrado na Infra, onde vive a impl.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Handlers de Domain Event. Vivem aqui, e não na Infra, porque Infra → Application seria
        // ciclo; o dispatcher os resolve pelo contêiner depois do commit.
        services.AddScoped<IDomainEventHandler<MembershipGrantedDomainEvent>, MembershipGrantedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<MembershipRevokedDomainEvent>, MembershipRevokedDomainEventHandler>();

        // Suspensão, reativação e produto mudam a MESMA coisa — quem enxerga aquele tenant e em
        // quais produtos —, então os quatro delegam ao mesmo sincronizador, que deriva o estado
        // desejado do agregado. Sem estes handlers os eventos eram emitidos e ninguém escutava:
        // suspender um tenant não cortava acesso nenhum, e habilitar produto não governava nada.
        services.AddScoped<TenantAccessSynchronizer>();
        services.AddScoped<IDomainEventHandler<TenantSuspendedDomainEvent>, TenantSuspendedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<TenantReactivatedDomainEvent>, TenantReactivatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ProductActivatedDomainEvent>, ProductActivatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ProductDeactivatedDomainEvent>, ProductDeactivatedDomainEventHandler>();

        // Query side (CQRS): interface chamada direto pelo controller, fora do mediator.
        services.AddScoped<ITenantQueries, TenantQueries>();

        return services;
    }
}
