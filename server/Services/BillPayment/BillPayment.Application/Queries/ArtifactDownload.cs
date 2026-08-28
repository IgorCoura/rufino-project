namespace BillPayment.Application.Queries;

using BillPayment.Domain.Ports;

/// <summary>
/// O documento original pronto para ser servido: o fluxo, o tipo de mídia e o nome sugerido.
/// </summary>
/// <remarks>
/// <para>
/// Existe para que o controller não tenha que decidir nada sobre o arquivo. Tipo de mídia e nome
/// são <strong>resolvidos aqui</strong>, com as fontes em ordem de confiabilidade, porque a
/// alternativa é cada endpoint improvisar a sua — e improvisar "é PDF" foi o erro medido em
/// 2026-08-11 do outro lado da cascata.
/// </para>
/// <para>
/// Não é Value Object: carrega um <c>Stream</c>, que tem estado e dono. É contrato de leitura,
/// irmão do <see cref="StoredArtifact"/> da porta.
/// </para>
/// </remarks>
public sealed record ArtifactDownload(Stream Content, string ContentType, string FileName, long? Length)
    : IDisposable
{
    /// <summary>Usado quando nenhuma fonte soube dizer o tipo. Faz o navegador baixar em vez de adivinhar.</summary>
    public const string FALLBACK_CONTENT_TYPE = "application/octet-stream";

    /// <summary>
    /// Se o que vai ser servido é uma cópia sem senha de um original cifrado.
    /// </summary>
    /// <remarks>
    /// Existe para a trilha, não para o corpo da resposta: entregar um documento reescrito é
    /// fato diferente de entregar o arquivo como ele chegou, e quem audita precisa distinguir os
    /// dois. Continua valendo que <strong>nem a senha nem o campo que a derivou saem por aqui</strong>.
    /// </remarks>
    public bool Unlocked { get; init; }

    /// <summary>
    /// Monta o download a partir do que o armazenamento devolveu.
    /// </summary>
    /// <param name="artifact">O que veio do balde.</param>
    /// <param name="declaredContentType">
    /// O tipo que o provedor declarou na ingestão, guardado no <c>CaptureItem</c>. Entra como
    /// segunda opção: o balde é a fonte primária porque é o único lado que a <c>Bill</c> tem.
    /// </param>
    /// <param name="fallbackFileName">Nome usado quando o artefato não trouxe um.</param>
    public static ArtifactDownload From(
        StoredArtifact artifact,
        string? declaredContentType,
        string fallbackFileName)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var contentType = FirstNonBlank(artifact.ContentType, declaredContentType) ?? FALLBACK_CONTENT_TYPE;

        return new ArtifactDownload(
            artifact.Content,
            contentType,
            EnsureExtension(fallbackFileName, contentType),
            artifact.Length);
    }

    /// <summary>Libera o fluxo subjacente.</summary>
    public void Dispose() => Content.Dispose();

    private static string? FirstNonBlank(params string?[] candidates)
        => candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))?.Trim();

    /// <summary>
    /// Garante extensão coerente com o tipo servido.
    /// </summary>
    /// <remarks>
    /// O nome do anexo no Microsoft Graph pode vir sem extensão nenhuma, e um arquivo salvo como
    /// "documento" não abre com dois cliques em nenhum sistema operacional.
    /// </remarks>
    private static string EnsureExtension(string fileName, string contentType)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "documento" : fileName.Trim();
        var extension = ExtensionFor(contentType);

        if (extension is null || name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            return name;

        return name + extension;
    }

    private static string? ExtensionFor(string contentType) => contentType.Split(';')[0].Trim() switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        "text/html" => ".html",
        _ => null,
    };
}
