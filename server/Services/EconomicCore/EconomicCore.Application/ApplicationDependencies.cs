namespace EconomicCore.Application;

using EconomicCore.Application.Behaviors;
using EconomicCore.Application.Mediator;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationDependencies
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        // Mediator próprio (sem MediatR) — escaneia handlers e behaviors do assembly da Application.
        services.AddCustomMediator(typeof(ApplicationDependencies).Assembly);

        // LoggingBehavior é o behavior mais externo e o único ativo nesta fase: nenhum command é
        // multi-aggregate, então não há TransactionBehavior. IRequestManager (idempotência) é
        // registrado na Infra, onde vive sua implementação sobre o DbContext.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
