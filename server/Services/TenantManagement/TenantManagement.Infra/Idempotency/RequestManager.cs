namespace TenantManagement.Infra.Idempotency;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementação da porta de idempotência sobre a tabela <c>client_requests</c>.
/// Compartilha o <see cref="TenantManagementDbContext"/> Scoped com o handler real: o
/// <c>Add</c> não commita, então a marca persiste na mesma transação do comando.
/// A PK em <c>Id</c> faz duplicatas concorrentes colidirem no banco.
/// </summary>
public sealed class RequestManager(TenantManagementDbContext db, TimeProvider timeProvider) : IRequestManager
{
    public Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken = default)
        => db.ClientRequests.AsNoTracking().AnyAsync(r => r.Id == id, cancellationToken);

    public Task CreateRequestForCommandAsync<TCommand>(Guid id, CancellationToken cancellationToken = default)
    {
        db.ClientRequests.Add(new ClientRequest(id, typeof(TCommand).Name, timeProvider.GetUtcNow().UtcDateTime));
        return Task.CompletedTask;
    }
}
