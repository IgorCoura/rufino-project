namespace BillPayment.Application.Behaviors;

using System.Diagnostics;
using BillPayment.Application.Mediator;
using Microsoft.Extensions.Logging;

/// <summary>
/// Behavior mais externo: loga início, fim e duração de cada request que passa
/// pelo mediator. Exceções propagam — quem as registra é o filtro da API.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Resolvido uma vez por par (TRequest, TResponse) fechado — campo estático de tipo genérico.
    private static readonly string RequestName = ResolveRequestName();

    /// <summary>
    /// Nome do Command de verdade, não o do embrulho. Toda escrita chega como
    /// <see cref="IdentifiedCommand{TCommand,TResult}"/>, então <c>typeof(TRequest).Name</c> devolvia
    /// <c>IdentifiedCommand`2</c> em 100% delas — a duração era medida e atribuída a um nome que não
    /// distingue uma operação da outra.
    /// </summary>
    private static string ResolveRequestName()
    {
        var type = typeof(TRequest);

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IdentifiedCommand<,>)
            ? type.GetGenericArguments()[0].Name
            : type.Name;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Handling {RequestName}", RequestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", RequestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
