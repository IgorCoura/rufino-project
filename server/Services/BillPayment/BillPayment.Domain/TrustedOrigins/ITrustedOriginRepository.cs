namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este
/// repositório não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
public interface ITrustedOriginRepository
{
    Task AddAsync(TrustedOrigin origin, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<TrustedOrigin?> GetAsync(TenantId tenantId, TrustedOriginId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade por (tenant, tipo, valor). O valor precisa vir normalizado.</summary>
    Task<bool> ExistsAsync(
        TenantId tenantId,
        OriginKind kind,
        string normalizedValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a origem de um remetente respeitando a precedência: endereço exato antes
    /// de domínio. Devolve <c>null</c> quando a origem é desconhecida — que é estado
    /// válido e comum, não erro.
    /// </summary>
    Task<TrustedOrigin?> ResolveBySenderAsync(
        TenantId tenantId,
        string senderAddress,
        CancellationToken cancellationToken = default);

    void Remove(TrustedOrigin origin);
}
