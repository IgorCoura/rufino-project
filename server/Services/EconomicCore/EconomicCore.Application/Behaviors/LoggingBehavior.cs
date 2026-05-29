namespace EconomicCore.Application.Behaviors;

using System.Diagnostics;
using EconomicCore.Application.Mediator;
using Microsoft.Extensions.Logging;

/// <summary>
/// Behavior mais externo: loga início, fim e duração de cada request que passa
/// pelo mediator. Exceções propagam — quem as registra é o filtro da API.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
