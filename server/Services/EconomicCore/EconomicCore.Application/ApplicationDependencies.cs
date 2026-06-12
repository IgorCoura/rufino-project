namespace EconomicCore.Application;

using EconomicCore.Application.Behaviors;
using EconomicCore.Application.Mediator;
using EconomicCore.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationDependencies
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        // Mediator próprio (sem MediatR) — escaneia handlers e behaviors do assembly da Application.
        services.AddCustomMediator(typeof(ApplicationDependencies).Assembly);

        // LoggingBehavior é o behavior mais externo e o único ativo nesta fase. RegisterLatePaymentEvent
        // é o único command multi-aggregate (marker IMultiAggregateCommand) e segue sem TransactionBehavior
        // porque há exatamente um SaveEntitiesAsync por handler — a transação implícita do EF cobre tudo.
        // IRequestManager (idempotência) é registrado na Infra, onde vive sua implementação sobre o DbContext.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Query side (CQRS): interfaces de leitura chamadas direto pelo controller, fora do mediator.
        services.AddScoped<IReportQueries, ReportQueries>();

        return services;
    }
}
