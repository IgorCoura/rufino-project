namespace BillPayment.Infra.Storage;

using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Substituto usado quando não há armazenamento configurado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Falha em toda operação, de propósito</strong> — inclusive na leitura. É o mesmo
/// princípio do cofre sem master key: guardar em lugar nenhum sem avisar faria o sistema pagar
/// um boleto cujo original ninguém consegue mais recuperar, e a ausência só apareceria na
/// auditoria, tarde demais.
/// </para>
/// <para>
/// A consequência prática é que o processamento de um artefato falha alto até alguém configurar
/// o `Storage`. Isso é preferível a processar e perder o comprovante.
/// </para>
/// </remarks>
internal sealed class UnconfiguredAttachmentStorage : IAttachmentStorage
{
    private const string MESSAGE =
        "Armazenamento de artefatos não configurado: defina Storage:ServiceUrl, AccessKey e SecretKey.";

    public Task<string> StoreAsync(
        TenantId tenantId,
        string fileName,
        string? contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException(MESSAGE);

    public Task<ReadOnlyMemory<byte>> RetrieveAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException(MESSAGE);

    /// <summary>
    /// A única operação tolerante: apagar o que nunca foi guardado já satisfaz o objetivo da
    /// purga, e fazê-la falhar travaria a limpeza de itens que não têm arquivo nenhum.
    /// </summary>
    public Task RemoveAsync(TenantId tenantId, string storageKey, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
