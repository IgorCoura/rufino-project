namespace BillPayment.Domain.SeedWork;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// O livro de requisições já processadas — a porta da idempotência por <c>x-requestid</c>.
/// </summary>
/// <remarks>
/// A marca é por <strong>tenant + id + comando</strong> (2026-08-28). Só pelo id, um
/// <c>x-requestid</c> de um tenant "consumia" o id para todos os outros, e o mesmo id reusado
/// num comando diferente do mesmo tenant era engolido como duplicata — sem erro, sem efeito.
/// </remarks>
public interface IRequestManager
{
    Task<bool> ExistAsync<TCommand>(TenantId tenantId, Guid id, CancellationToken cancellationToken = default);

    Task CreateRequestForCommandAsync<TCommand>(TenantId tenantId, Guid id, CancellationToken cancellationToken = default);
}
