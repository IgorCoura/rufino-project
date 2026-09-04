namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao livro-caixa da captura. Toda busca filtra por <see cref="TenantId"/> —
/// nenhuma travessia de tenant aqui.
/// </summary>
public interface ICapturedMessageRepository
{
    Task AddAsync(CapturedMessage message, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<CapturedMessage?> GetAsync(
        TenantId tenantId,
        CapturedMessageId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O registro daquela mensagem naquela fonte, para o processamento gravar o desfecho.
    /// </summary>
    /// <remarks>
    /// A chave <strong>não</strong> inclui o artefato, ao contrário da do <c>CaptureItem</c>:
    /// aqui o agregado é a mensagem inteira, e os anexos são entidades dentro dela.
    /// </remarks>
    Task<CapturedMessage?> FindByExternalMessageIdAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registros vencidos pela janela de retenção, <strong>já excluindo os que produziram
    /// boleto</strong> — trilha de auditoria de pagamento não expira.
    /// </summary>
    Task<IReadOnlyList<CapturedMessage>> ListPurgeableAsync(
        TenantId tenantId,
        DateTime olderThan,
        int limit,
        CancellationToken cancellationToken = default);

    void Remove(CapturedMessage message);
}
