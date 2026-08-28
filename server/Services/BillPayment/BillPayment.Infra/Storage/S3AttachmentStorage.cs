namespace BillPayment.Infra.Storage;

using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Guarda o artefato original em serviço compatível com S3.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A chave começa pelo tenant, e isso é isolamento, não organização.</strong> Toda
/// operação recompõe a chave a partir do <c>TenantId</c> de quem chamou, então uma chave vazada
/// de outro tenant não recupera nada: o prefixo não confere. É a mesma disciplina do filtro por
/// <c>TenantId</c> em toda query do BC.
/// </para>
/// <para>
/// <strong>Não cifra aqui.</strong> A cifra em repouso é do serviço de armazenamento (server-side
/// encryption do Garage/S3); duplicá-la na aplicação criaria um segundo lugar para perder a
/// chave sem ganhar defesa contra o cenário que importa — quem alcança o balde alcança o
/// processo que decifraria.
/// </para>
/// </remarks>
internal sealed class S3AttachmentStorage(
    IAmazonS3 client,
    IOptions<StorageOptions> options,
    ILogger<S3AttachmentStorage> logger) : IAttachmentStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> StoreAsync(
        TenantId tenantId,
        string fileName,
        string? contentType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var key = ComposeKey(tenantId, fileName);

        using var stream = new MemoryStream(content.ToArray(), writable: false);

        await client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = stream,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            },
            cancellationToken);

        return key;
    }

    public async Task<ReadOnlyMemory<byte>> RetrieveAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken)
    {
        EnsureBelongsToTenant(tenantId, storageKey);

        using var response = await client.GetObjectAsync(
            new GetObjectRequest { BucketName = _options.Bucket, Key = storageKey }, cancellationToken);

        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);

        return buffer.ToArray();
    }

    public async Task<StoredArtifact?> OpenAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken)
    {
        EnsureBelongsToTenant(tenantId, storageKey);

        try
        {
            var response = await client.GetObjectAsync(
                new GetObjectRequest { BucketName = _options.Bucket, Key = storageKey }, cancellationToken);

            // O response NÃO é liberado aqui de propósito: quem fecha o fluxo é o StoredArtifact,
            // nas mãos de quem o consome. Um `using` neste escopo entregaria um Stream já morto.
            return new StoredArtifact(
                response.ResponseStream,
                response.Headers.ContentType,
                response.ContentLength >= 0 ? response.ContentLength : null);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Chave órfã: o banco aponta para objeto que não está mais no balde. Vira "não há
            // documento" para quem pediu, e uma linha de log para quem cuida da integridade.
            logger.LogWarning(ex, "Artefato ausente no armazenamento para uma chave ainda registrada.");
            return null;
        }
    }

    public async Task RemoveAsync(TenantId tenantId, string storageKey, CancellationToken cancellationToken)
    {
        EnsureBelongsToTenant(tenantId, storageKey);

        try
        {
            await client.DeleteObjectAsync(
                new DeleteObjectRequest { BucketName = _options.Bucket, Key = storageKey }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Idempotente por contrato: a purga por desfecho pode passar duas vezes pelo mesmo
            // item, e o objetivo dela — o arquivo não existir — já está satisfeito.
            logger.LogDebug(ex, "Artefato já ausente no armazenamento.");
        }
    }

    /// <summary>
    /// <c>tenants/{tenant}/captures/{ano}/{mês}/{id}-{nome}</c>. O particionamento por data
    /// existe porque listagem de balde com dezenas de milhares de objetos num prefixo só fica
    /// impraticável quando alguém precisa auditar um período.
    /// </summary>
    private static string ComposeKey(TenantId tenantId, string fileName)
    {
        var now = DateTime.UtcNow;
        var safe = Sanitize(fileName);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix(tenantId)}{now:yyyy}/{now:MM}/{Guid.CreateVersion7():N}-{safe}");
    }

    private static string Prefix(TenantId tenantId) => $"tenants/{tenantId.Value:N}/captures/";

    /// <summary>
    /// Recusa chave que não pertence ao tenant que pediu.
    /// </summary>
    /// <remarks>
    /// A chave vem do banco, então em fluxo normal ela sempre confere. A checagem existe para o
    /// dia em que alguém aceitar uma chave vinda de request — aí ela é a diferença entre um
    /// parâmetro e um vazamento entre contas.
    /// </remarks>
    private static void EnsureBelongsToTenant(TenantId tenantId, string storageKey)
    {
        if (!storageKey.StartsWith(Prefix(tenantId), StringComparison.Ordinal))
            throw new UnauthorizedAccessException("A chave de armazenamento não pertence a este tenant.");
    }

    /// <summary>
    /// O nome do arquivo vem de fora e vai para um caminho: barra, ponto-ponto e caractere de
    /// controle saem antes de chegar perto de qualquer sistema de arquivos.
    /// </summary>
    private static string Sanitize(string fileName)
    {
        var trimmed = (fileName ?? string.Empty).Trim();
        var safe = new string(trimmed
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_')
            .ToArray());

        safe = safe.Replace("..", ".", StringComparison.Ordinal).Trim('.');

        return string.IsNullOrEmpty(safe) ? "artefato" : safe[..Math.Min(safe.Length, 80)];
    }
}
