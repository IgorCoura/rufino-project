namespace BillPayment.Infra.Idempotency;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementação da porta de idempotência sobre a tabela <c>client_requests</c>.
/// Compartilha o <see cref="BillPaymentDbContext"/> Scoped com o handler real: o
/// <c>Add</c> não commita, então a marca persiste na mesma transação do comando.
/// A PK composta <c>(tenant_id, id, name)</c> faz duplicatas concorrentes colidirem no banco —
/// e só duplicatas de verdade: mesmo id em outro tenant, ou em outro comando, é outra marca.
/// </summary>
public sealed class RequestManager(BillPaymentDbContext db, TimeProvider clock) : IRequestManager
{
    public Task<bool> ExistAsync<TCommand>(TenantId tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var name = typeof(TCommand).Name;

        return db.ClientRequests
            .AsNoTracking()
            .AnyAsync(r => r.TenantId == tenantId.Value && r.Id == id && r.Name == name, cancellationToken);
    }

    public Task CreateRequestForCommandAsync<TCommand>(TenantId tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        db.ClientRequests.Add(new ClientRequest(tenantId.Value, id, typeof(TCommand).Name, clock.GetUtcNow().UtcDateTime));
        return Task.CompletedTask;
    }
}
