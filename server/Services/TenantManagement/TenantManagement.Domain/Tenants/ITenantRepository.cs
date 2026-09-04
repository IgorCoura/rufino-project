namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Diferente dos produtos, aqui <strong>não existe filtro por
/// tenant</strong>: este BC é o back-office que administra todos eles. Quem decide o que cada
/// operador pode ver é a autorização na borda, não o repositório.
/// </summary>
public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, com produtos e vínculos, para mutação.</summary>
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade: um documento fiscal, um tenant.</summary>
    Task<bool> ExistsByPrimaryTaxIdAsync(TaxId taxId, CancellationToken cancellationToken = default);
}
