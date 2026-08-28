namespace BillPayment.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Armazenamento de artefatos em memória.
/// </summary>
/// <remarks>
/// <para>
/// Substitui o balde S3 porque a suíte não deve depender de serviço externo nem carregar
/// credencial de armazenamento. O que está sob teste é a <strong>cadeia</strong> — baixar,
/// extrair, triar, reter —, e a decisão de guardar ou não guardar é observável aqui do mesmo
/// jeito que seria num balde de verdade.
/// </para>
/// <para>
/// A chave é composta com o tenant no prefixo, como no adapter real, para que um teste de
/// isolamento continue significando a mesma coisa.
/// </para>
/// </remarks>
internal sealed class InMemoryAttachmentStorage : IAttachmentStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _contentTypes = new(StringComparer.Ordinal);

    public int Count => _objects.Count;

    public bool Contains(string storageKey) => _objects.ContainsKey(storageKey);

    public Task<string> StoreAsync(
        TenantId tenantId,
        string fileName,
        string? contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var key = $"tenants/{tenantId.Value:N}/captures/{Guid.CreateVersion7():N}-{fileName}";
        _objects[key] = content.ToArray();

        // O tipo é guardado junto, como o balde real guarda: é dele que a leitura do documento
        // tira o Content-Type, porque a Bill não persiste tipo nenhum.
        if (!string.IsNullOrWhiteSpace(contentType))
            _contentTypes[key] = contentType;

        return Task.FromResult(key);
    }

    public Task<ReadOnlyMemory<byte>> RetrieveAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken)
        => _objects.TryGetValue(storageKey, out var bytes)
            ? Task.FromResult<ReadOnlyMemory<byte>>(bytes)
            : throw new FileNotFoundException("Artefato não encontrado no armazenamento de teste.", storageKey);

    /// <summary>
    /// Devolve <c>null</c> para chave ausente, como o contrato exige — quem serve o documento
    /// para uma pessoa precisa de 404, não de exceção.
    /// </summary>
    public Task<StoredArtifact?> OpenAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (!_objects.TryGetValue(storageKey, out var bytes))
            return Task.FromResult<StoredArtifact?>(null);

        var contentType = _contentTypes.GetValueOrDefault(storageKey);

        return Task.FromResult<StoredArtifact?>(
            new StoredArtifact(new MemoryStream(bytes, writable: false), contentType, bytes.Length));
    }

    /// <summary>Idempotente, como o contrato exige — a purga por desfecho pode passar duas vezes.</summary>
    public Task RemoveAsync(TenantId tenantId, string storageKey, CancellationToken cancellationToken)
    {
        _objects.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}
