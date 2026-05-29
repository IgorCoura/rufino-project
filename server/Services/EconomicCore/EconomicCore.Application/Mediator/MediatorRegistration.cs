namespace EconomicCore.Application.Mediator;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public static class MediatorRegistration
{
    /// <summary>
    /// Registra o <see cref="IMediator"/> e escaneia os assemblies por
    /// <see cref="IRequestHandler{TRequest,TResponse}"/> e
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// </summary>
    public static IServiceCollection AddCustomMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();

        foreach (var assembly in assemblies)
        {
            // Exatamente um handler por request: duplicata é erro de configuração e falha no startup.
            RegisterClosedGenerics(services, assembly, typeof(IRequestHandler<,>), singlePerService: true);
            // Múltiplos behaviors podem fechar o mesmo (TRequest,TResponse) — é o próprio pipeline.
            RegisterClosedGenerics(services, assembly, typeof(IPipelineBehavior<,>), singlePerService: false);
        }

        return services;
    }

    private static void RegisterClosedGenerics(IServiceCollection services, Assembly assembly, Type openGeneric, bool singlePerService)
    {
        var registrations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric)
                .Select(i => (Service: i, Implementation: t)))
            .ToList();

        if (singlePerService)
        {
            var duplicate = registrations
                .GroupBy(r => r.Service)
                .FirstOrDefault(g => g.Select(r => r.Implementation).Distinct().Count() > 1);

            if (duplicate is not null)
            {
                var implementations = string.Join(", ", duplicate.Select(r => r.Implementation.Name).Distinct());
                throw new InvalidOperationException(
                    $"Múltiplos handlers para {duplicate.Key}: {implementations}. Cada request deve ter exatamente um IRequestHandler.");
            }
        }

        foreach (var (service, implementation) in registrations)
        {
            services.AddScoped(service, implementation);
        }
    }
}
