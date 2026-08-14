namespace TenantManagement.Infra;

using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using TenantManagement.Infra.Events;
using TenantManagement.Infra.Identity;
using TenantManagement.Infra.Idempotency;
using TenantManagement.Infra.Persistence;
using TenantManagement.Infra.Repositories;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class InfraDependencies
{
    public const string CONNECTION_STRING_NAME = "TenantManagement";

    public static IServiceCollection AddInfraDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TenantManagementDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString(CONNECTION_STRING_NAME),
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure();

                    // O histórico de migrações mora no schema do BC, e não no public. Quem
                    // construir o DbContext fora do DI precisa repetir esta linha — sem ela o
                    // EF procura a tabela no lugar errado e conclui que nada foi aplicado.
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", TenantManagementDbContext.DEFAULT_SCHEMA);
                });

            options.UseExceptionProcessor();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TenantManagementDbContext>());
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRequestManager, RequestManager>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services.AddTenantProvisioning(configuration);
    }

    /// <summary>
    /// Registra o adapter do provedor de identidade — ou o substituto que falha alto quando
    /// ele não está configurado. Nunca um dublê silencioso: acesso que ninguém concedeu não
    /// pode ser reportado como concedido.
    /// </summary>
    private static IServiceCollection AddTenantProvisioning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TenantProvisioningOptions>(
            configuration.GetSection(TenantProvisioningOptions.SectionName));

        var options = new TenantProvisioningOptions();
        configuration.GetSection(TenantProvisioningOptions.SectionName).Bind(options);

        if (!options.IsConfigured)
        {
            services.AddScoped<ITenantAccessProvisioner, UnconfiguredTenantAccessProvisioner>();
            return services;
        }

        services.AddHttpClient<ITenantAccessProvisioner, KeycloakTenantAccessProvisioner>((sp, client) =>
        {
            var current = sp.GetRequiredService<IOptions<TenantProvisioningOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(current.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        return services;
    }
}
