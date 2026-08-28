namespace BillPayment.Application.Queries.Bills;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única
/// exceção autorizada a tocar a Infra, conforme registrado no CLAUDE.md do BC.
/// </summary>
public interface IBillQueries
{
    Task<BillPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<BillDto?> GetAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O detalhe que a tela de aprovação consome: o boleto, o beneficiário que a consulta
    /// devolveu, e as doze verificações com evidência.
    /// </summary>
    Task<BillDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O documento original que deu origem ao boleto, para o aprovador conferir o papel contra as
    /// verificações.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Devolve <c>null</c> quando o boleto não é deste tenant, quando não veio de um artefato
    /// guardado (importação manual nasce só com os dígitos) ou quando a chave ficou órfã. A tela
    /// trata os três como "não há documento".
    /// </para>
    /// <para>
    /// <strong>Não expõe a linha digitável nem o payload Pix</strong> — quem tem os dígitos,
    /// paga. O que sai aqui é o documento, que é o comprovante do que o sistema viu quando
    /// decidiu.
    /// </para>
    /// <para>
    /// <strong>PDF cifrado sai destravado.</strong> O que fica guardado é o original, e o
    /// emissor que protege por senha a derivou do documento do pagador — que o cadastro do tenant
    /// já tem. A cópia legível nasce aqui, a cada leitura; a senha continua sem sair (ADR-009).
    /// </para>
    /// </remarks>
    Task<ArtifactDownload?> GetArtifactAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);
}
