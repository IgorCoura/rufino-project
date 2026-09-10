namespace BillPayment.Application.Queries;

using BillPayment.Domain.Extraction;
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
    /// <param name="measuredContentType">
    /// O tipo lido nos bytes do próprio arquivo, quando alguém já os espiou. Vem <strong>na
    /// frente de todos</strong>: é a única fonte que não é declaração de terceiro.
    /// </param>
    public static ArtifactDownload From(
        StoredArtifact artifact,
        string? declaredContentType,
        string fallbackFileName,
        string? measuredContentType = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var contentType = FirstNonBlank(measuredContentType, artifact.ContentType, declaredContentType)
            ?? FALLBACK_CONTENT_TYPE;

        return new ArtifactDownload(
            artifact.Content,
            contentType,
            EnsureExtension(fallbackFileName, contentType),
            artifact.Length);
    }

    /// <summary>
    /// Abre o artefato <strong>conferindo nos bytes</strong> o que ele é, e só então monta o
    /// download.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É a porta única para servir um artefato guardado.</strong> O rótulo do balde é o
    /// que o provedor declarou lá atrás, e provedor erra: a Notredame Intermédica manda o boleto
    /// com <c>Content-Type: pdf</c>, o Exchange normaliza isso para
    /// <c>application/octet-stream</c>, e o app — que só sabe abrir <c>application/pdf</c> e
    /// <c>image/*</c> — mostrava "formato que o app não exibe" sobre um PDF perfeito.
    /// </para>
    /// <para>
    /// <strong>Corrige o que já está guardado, não só o que ainda vai entrar.</strong> Conferir
    /// aqui é o que dispensa migração de balde: o objeto continua com o rótulo errado gravado, e
    /// mesmo assim é servido pelo que ele é.
    /// </para>
    /// <para>
    /// <strong>Sem abrir mão do fluxo.</strong> Só <see cref="DocumentMagic.PREFIX_LENGTH"/>
    /// bytes são lidos adiantado; eles voltam para a frente do fluxo, e o resto continua vindo do
    /// balde sob demanda, como antes.
    /// </para>
    /// </remarks>
    public static async Task<ArtifactDownload> OpenAsync(
        StoredArtifact artifact,
        string? declaredContentType,
        string fallbackFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var prefix = new byte[DocumentMagic.PREFIX_LENGTH];
        var read = await ReadAtLeastAsync(artifact.Content, prefix, cancellationToken);

        var measured = DocumentMagic.MediaTypeOf(prefix.AsSpan(0, read));
        var rewound = new PrefixedStream(prefix.AsMemory(0, read), artifact.Content);

        return From(artifact with { Content = rewound }, declaredContentType, fallbackFileName, measured);
    }

    /// <summary>
    /// Enche <paramref name="buffer"/> até onde o fluxo der, e devolve quanto veio.
    /// </summary>
    /// <remarks>
    /// Um <c>ReadAsync</c> só não basta: fluxo de rede devolve o que já chegou, e um retorno
    /// curto seria lido como "arquivo pequeno demais para ter assinatura". Fim de fluxo é o único
    /// motivo aceito para parar antes.
    /// </remarks>
    private static async Task<int> ReadAtLeastAsync(
        Stream source,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await source.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (read == 0)
                break;

            total += read;
        }

        return total;
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
