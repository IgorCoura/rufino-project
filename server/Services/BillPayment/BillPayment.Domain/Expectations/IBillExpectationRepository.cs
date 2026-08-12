namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este repositório
/// não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
public interface IBillExpectationRepository
{
    Task AddAsync(BillExpectation expectation, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<BillExpectation?> GetAsync(
        TenantId tenantId, BillExpectationId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade por (tenant, beneficiário, referência de conta).</summary>
    Task<bool> ExistsAsync(
        TenantId tenantId, PayeeId payeeId, string accountReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// As expectativas que o beneficiário responde, para o casamento de um boleto que chegou.
    /// </summary>
    Task<IReadOnlyCollection<BillExpectation>> ListByPayeeAsync(
        TenantId tenantId, PayeeId payeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A fila do job: expectativas ativas de todos os tenants, com os ciclos carregados.
    /// </summary>
    /// <remarks>
    /// <strong>Não é travessia de tenant.</strong> Quem chama é o worker, que não age em nome de
    /// nenhum usuário e não projeta nada para tela — mesma natureza da varredura de caixas. O
    /// <c>TenantId</c> continua em cada agregado e acompanha todo efeito que sair daqui.
    /// </remarks>
    Task<IReadOnlyCollection<BillExpectation>> ListActiveForSweepAsync(
        int batchSize, CancellationToken cancellationToken = default);

    void Remove(BillExpectation expectation);
}
