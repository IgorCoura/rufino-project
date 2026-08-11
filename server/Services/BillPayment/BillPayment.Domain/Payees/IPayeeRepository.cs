namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este
/// repositório não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
public interface IPayeeRepository
{
    Task AddAsync(Payee payee, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<Payee?> GetAsync(TenantId tenantId, PayeeId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade por (tenant, documento). O documento é a identidade estável do beneficiário.</summary>
    Task<bool> ExistsAsync(TenantId tenantId, TaxId taxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Todos os beneficiários do tenant, para a resolução do beneficiário consultado.
    /// </summary>
    /// <remarks>
    /// Traz <strong>inclusive os inativos</strong>: beneficiário desativado que volta a emitir
    /// boleto precisa reprovar por <c>payee_inactive</c>, e não passar por "não cadastrado".
    /// A carga completa é o que a detecção de sósia exige — ela compara o nome consultado
    /// contra o conjunto inteiro, não contra um candidato já escolhido.
    /// </remarks>
    Task<IReadOnlyCollection<Payee>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    void Remove(Payee payee);
}
