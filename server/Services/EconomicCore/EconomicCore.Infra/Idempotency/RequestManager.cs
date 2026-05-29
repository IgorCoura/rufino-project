namespace EconomicCore.Infra.Idempotency;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementação da porta de idempotência sobre a tabela <c>client_requests</c>.
/// Compartilha o <see cref="EconomicCoreDbContext"/> Scoped com o handler real: o
/// <c>Add</c> não commita, então a marca persiste na mesma transação do comando.
/// A PK em <c>Id</c> faz duplicatas concorrentes colidirem no banco.
/// </summary>
public sealed class RequestManager(EconomicCoreDbContext db) : IRequestManager
{
    public Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken = default)
        => db.ClientRequests.AsNoTracking().AnyAsync(r => r.Id == id, cancellationToken);

    public Task CreateRequestForCommandAsync<TCommand>(Guid id, CancellationToken cancellationToken = default)
    {
        db.ClientRequests.Add(new ClientRequest(id, typeof(TCommand).Name, DateTime.UtcNow));
        return Task.CompletedTask;
    }
}
