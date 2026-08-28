namespace BillPayment.Application.Mediator;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler base de idempotência: checa se o requestId já foi processado antes de
/// delegar ao handler real. Cada Command tem uma subclasse concreta no mesmo
/// arquivo do seu Handler, sobrescrevendo <see cref="CreateResultForDuplicateRequest"/>.
/// </summary>
/// <remarks>
/// A marca é por <c>(tenant, id, comando)</c> — por isso <typeparamref name="TCommand"/> precisa
/// ser <see cref="ITenantScopedCommand"/>: é daí que o tenant sai, sem o pipeline conhecer o
/// tipo concreto. Até 2026-08-28 a marca era só pelo id.
/// </remarks>
public abstract class IdentifiedCommandHandler<TCommand, TResult>(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger logger)
    : IRequestHandler<IdentifiedCommand<TCommand, TResult>, TResult>
    where TCommand : IRequest<TResult>, ITenantScopedCommand
{
    /// <summary>
    /// Resposta devolvida quando o request já foi processado — um valor neutro
    /// semanticamente seguro (Guid.Empty, strings vazias). O cliente não deve
    /// distinguir "processou agora" de "já tinha processado".
    /// </summary>
    protected abstract TResult CreateResultForDuplicateRequest();

    public async Task<TResult> Handle(IdentifiedCommand<TCommand, TResult> request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.Command.TenantId);

        if (await requestManager.ExistAsync<TCommand>(tenantId, request.Id, cancellationToken))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Request idempotente ignorada: {RequestId} para {CommandType}",
                    request.Id, typeof(TCommand).Name);

            return CreateResultForDuplicateRequest();
        }

        // Não commita aqui — a marca entra na transação do handler real (mesmo DbContext Scoped).
        await requestManager.CreateRequestForCommandAsync<TCommand>(tenantId, request.Id, cancellationToken);

        try
        {
            return await mediator.Send(request.Command, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Corrida: outro request com o mesmo x-requestid cometeu a marca primeiro e a PK de
            // client_requests reverteu esta transação (inclusive o efeito do comando). Só tratamos
            // como duplicata se a marca de fato já existe — caso contrário é outra falha de banco.
            if (!await requestManager.ExistAsync<TCommand>(tenantId, request.Id, cancellationToken))
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
