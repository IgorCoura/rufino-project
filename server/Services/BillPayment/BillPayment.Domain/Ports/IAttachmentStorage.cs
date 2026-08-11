namespace BillPayment.Domain.Ports;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Guarda o artefato original de um item capturado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O documento é guardado como recebido</strong> — é o comprovante do que o sistema viu
/// quando decidiu pagar, e reprocessar depois exige o original, não uma versão normalizada.
/// </para>
/// <para>
/// <strong>Só o que virou boleto merece ficar.</strong> A cascata só conclui que um anexo não
/// interessa <em>depois</em> de já tê-lo baixado, então guardar tudo transformaria o balde num
/// depósito de documento pessoal — medido na caixa real: 8 de 11 anexos da primeira página não
/// eram conta a pagar, incluindo CNH e contrato. Daí <see cref="RemoveAsync"/> existir e ser
/// chamado pela purga por desfecho, não só por exclusão manual.
/// </para>
/// <para>
/// A chave é opaca para o Domain: quem a compõe é o adapter, e nada no modelo depende do formato
/// dela além de ser estável.
/// </para>
/// </remarks>
public interface IAttachmentStorage
{
    /// <summary>Guarda os bytes e devolve a chave para recuperá-los.</summary>
    Task<string> StoreAsync(
        TenantId tenantId,
        string fileName,
        string? contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);

    /// <summary>Recupera o original. Lança quando a chave não existe — é falha de integridade.</summary>
    Task<ReadOnlyMemory<byte>> RetrieveAsync(TenantId tenantId, string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Apaga o artefato. <strong>Idempotente</strong>: chave inexistente não é erro, porque a
    /// purga por desfecho pode passar duas vezes pelo mesmo item sem que isso seja problema.
    /// </summary>
    Task RemoveAsync(TenantId tenantId, string storageKey, CancellationToken cancellationToken);
}
