namespace EconomicCore.Infra;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Operational.EconomicResources.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Infra.Outbox;
using EconomicCore.Infra.Outbox.Handlers;
using EconomicCore.Infra.Persistence;
using EconomicCore.Infra.Repositories;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfraDependencies
{
    public static IServiceCollection AddInfraDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EconomicCoreDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("EconomicCore"), npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", EconomicCoreDbContext.DEFAULT_SCHEMA);
                npgsql.EnableRetryOnFailure(3);
            });
            options.UseExceptionProcessor();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EconomicCoreDbContext>());
        services.AddScoped<IEconomicEventRepository, EconomicEventRepository>();
        services.AddScoped<IEconomicResourceRepository, EconomicResourceRepository>();
        services.AddScoped<IEconomicAgentRepository, EconomicAgentRepository>();
        services.AddScoped<IEconomicContractRepository, EconomicContractRepository>();

        services.AddOutbox(configuration);

        return services;
    }

    private static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddDomainEventDispatcher();
        services.AddSingleton<IOutboxEventTypeResolver, OutboxEventTypeResolver>();
        services.AddSingleton<IOutboxProcessor, OutboxProcessor>();

        services.AddScoped<IDomainEventHandler<EconomicResourceRegistered>, EconomicResourceRegisteredAuditHandler>();

        var enabled = configuration.GetSection(OutboxOptions.SectionName).GetValue<bool?>("Enabled") ?? true;
        if (enabled)
            services.AddHostedService<OutboxBackgroundService>();

        return services;
    }

    // Public so the in-process dispatcher can be wired in isolation (e.g. tests) without the full outbox stack.
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
