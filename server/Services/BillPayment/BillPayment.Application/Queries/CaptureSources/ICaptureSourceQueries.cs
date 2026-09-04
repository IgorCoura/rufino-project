namespace BillPayment.Application.Queries.CaptureSources;

/// <summary>
/// Leitura das fontes do tenant. Um usuário <strong>nunca</strong> vê a fonte, o cursor, o
/// histórico de sincronização ou os itens de outro tenant — não há aqui nenhuma das três
/// travessias autorizadas do BC.
/// </summary>
public interface ICaptureSourceQueries
{
    Task<CaptureSourcePage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CaptureSourceDto?> GetAsync(Guid tenantId, Guid captureSourceId, CancellationToken cancellationToken = default);
}
