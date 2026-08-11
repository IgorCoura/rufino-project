namespace BillPayment.Domain.PayerProfiles;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. O cadastro fiscal é <strong>um por tenant</strong> — por isso
/// a busca é pelo <see cref="TenantId"/> e não pelo <see cref="PayerProfileId"/>: o tenant é a
/// chave natural, e expor busca por id abriria caminho para ler o cadastro de outra conta.
/// </summary>
public interface IPayerProfileRepository
{
    Task AddAsync(PayerProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se o tenant ainda não se cadastrou.</summary>
    Task<PayerProfile?> GetByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade: um cadastro fiscal por tenant.</summary>
    Task<bool> ExistsForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
