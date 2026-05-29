namespace EconomicCore.Application.Mediator;

using EconomicCore.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler base de idempotência: checa se o requestId já foi processado antes de
/// delegar ao handler real. Cada Command tem uma subclasse concreta no mesmo
/// arquivo do seu Handler, sobrescrevendo <see cref="CreateResultForDuplicateRequest"/>.
/// </summary>
public abstract class IdentifiedCommandHandler<TCommand, TResult>(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger logger)
    : IRequestHandler<IdentifiedCommand<TCommand, TResult>, TResult>
    where TCommand : IRequest<TResult>
{
    /// <summary>
    /// Resposta devolvida quando o request já foi processado — um valor neutro
    /// semanticamente seguro (Guid.Empty, strings vazias). O cliente não deve
    /// distinguir "processou agora" de "já tinha processado".
    /// </summary>
    protected abstract TResult CreateResultForDuplicateRequest();

    public async Task<TResult> Handle(IdentifiedCommand<TCommand, TResult> request, CancellationToken cancellationToken)
    {
        if (await requestManager.ExistAsync(request.Id, cancellationToken))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Request idempotente ignorada: {RequestId} para {CommandType}",
                    request.Id, typeof(TCommand).Name);

            return CreateResultForDuplicateRequest();
        }

        // Não commita aqui — a marca entra na transação do handler real (mesmo DbContext Scoped).
        await requestManager.CreateRequestForCommandAsync<TCommand>(request.Id, cancellationToken);

        try
        {
            return await mediator.Send(request.Command, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Corrida: outro request com o mesmo x-requestid cometeu a marca primeiro e a PK de
            // client_requests reverteu esta transação (inclusive o efeito do comando). Só tratamos
            // como duplicata se a marca de fato já existe — caso contrário é outra falha de banco.
            if (!await requestManager.ExistAsync(request.Id, cancellationToken))
                throw;

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    ex,
                    "Request idempotente concorrente ignorada: {RequestId} para {CommandType}",
                    request.Id, typeof(TCommand).Name);

            return CreateResultForDuplicateRequest();
        }
    }
}
