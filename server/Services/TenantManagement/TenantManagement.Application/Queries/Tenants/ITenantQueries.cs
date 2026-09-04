namespace TenantManagement.Application.Queries.Tenants;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única exceção
/// autorizada a tocar a Infra, mesma convenção dos outros BCs.
/// </summary>
public interface ITenantQueries
{
    Task<TenantPage> ListAsync(
        TenantListFilter filter,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<TenantDto?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Os tenants que este e-mail acessa. É a consulta que alimenta o seletor de contexto do
    /// cliente — e a única do BC que parte da pessoa, e não do tenant.
    /// </summary>
    Task<IReadOnlyList<MyTenantDto>> ListForMemberAsync(
        string email,
        CancellationToken cancellationToken = default);
}
