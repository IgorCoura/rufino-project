namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este
/// repositório não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
/// <remarks>
/// O isolamento aqui é literal: a mesma mensagem lida por duas fontes de dois tenants produz
/// <strong>dois</strong> itens distintos, e isso é correto (ADR-008). Nenhum método enxerga o
/// item do outro.
/// </remarks>
public interface ICaptureItemRepository
{
    Task AddAsync(CaptureItem item, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<CaptureItem?> GetAsync(TenantId tenantId, CaptureItemId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotência da ingestão: este artefato desta mensagem desta fonte já entrou?
    /// </summary>
    /// <remarks>
    /// A chave inclui <see cref="CaptureItem.ArtifactKey"/> porque um e-mail com três boletos
    /// gera três itens — checar só pelo id da mensagem descartaria dois deles.
    /// </remarks>
    Task<bool> ExistsAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Todos os itens de um e-mail — um por anexo —, <em>tracked</em>, para a recaptura reescrever
    /// cada um em cima do que existe em vez de apagar e recriar.
    /// </summary>
    Task<IReadOnlyList<CaptureItem>> ListByMessageAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procura um item já ingerido com o mesmo conteúdo, para o desfecho <c>Discarded</c>.
    /// </summary>
    /// <remarks>
    /// Dedup por conteúdo, e não por mensagem: o mesmo boleto reenviado noutra thread tem outro
    /// <c>ExternalMessageId</c> e o mesmo <c>ContentHash</c>. Devolve <c>null</c> quando é a
    /// primeira vez — que é o caso comum.
    /// </remarks>
    Task<CaptureItemId?> FindOriginalByContentHashAsync(
        TenantId tenantId,
        string contentHash,
        CaptureItemId excludingId,
        CancellationToken cancellationToken = default);

    void Remove(CaptureItem item);
}
