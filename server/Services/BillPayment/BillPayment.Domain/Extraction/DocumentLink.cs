namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Um link achado no corpo de uma mensagem, já reduzido ao endereço que seria realmente visitado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O <see cref="Url"/> é o alvo, não o que estava no <c>href</c>.</strong> Medido em
/// 2026-08-11 na caixa real: todo boleto por link chega embrulhado em rastreador de campanha
/// (<c>awstrack.me/L0/…</c>), e o endereço de verdade só aparece percent-encoded dentro do
/// caminho. Guardar o <c>href</c> cru faria a allowlist decidir sobre o domínio do rastreador —
/// que é o mesmo para o boleto legítimo e para qualquer coisa que alguém resolva embrulhar.
/// </para>
/// <para>
/// <strong>A porta faz parte da identidade.</strong> A SABESP publica o PDF da fatura em
/// <c>file-pdf.7az.com.br:<b>7446</b></c> — uma regra que assumisse 443 perderia exatamente o
/// único documento que hoje é alcançável por download direto.
/// </para>
/// <para>
/// <strong>Este endereço é tão sigiloso quanto o documento.</strong> As quatro URLs medidas
/// respondem <c>200</c> sem autenticação nenhuma: quem tem o link tem o boleto. Por isso ele nunca
/// entra em log e sai por API só sob o mesmo portão do ADR-008 que cobre o <c>StorageKey</c>.
/// </para>
/// </remarks>
public sealed class DocumentLink : ValueObject
{
    public const int URL_MAX_LENGTH = 2000;
    public const int LABEL_MAX_LENGTH = 200;

    /// <summary>O endereço final, absoluto e já desembrulhado.</summary>
    public string Url { get; }

    public string Host { get; }

    public int Port { get; }

    /// <summary>Caminho e consulta, em minúsculas — é sobre isto que a receita casa.</summary>
    public string PathAndQuery { get; }

    /// <summary>
    /// Texto da âncora, quando havia. É o que distingue "Acessar Boleto" de um ícone de rede
    /// social no mesmo e-mail.
    /// </summary>
    public string? Label { get; }

    /// <summary>Se o endereço veio de dentro de um rastreador de campanha.</summary>
    public bool WasWrapped { get; }

    private DocumentLink(string url, string host, int port, string pathAndQuery, string? label, bool wasWrapped)
    {
        Url = url;
        Host = host;
        Port = port;
        PathAndQuery = pathAndQuery;
        Label = label;
        WasWrapped = wasWrapped;
    }

    /// <summary>
    /// Constrói o link, ou devolve <c>null</c> quando o endereço não é utilizável.
    /// </summary>
    /// <remarks>
    /// <strong>Devolve <c>null</c> em vez de lançar</strong> porque um e-mail traz dezenas de
    /// <c>href</c>, e a maioria é <c>mailto:</c>, <c>tel:</c>, <c>#</c> ou lixo de template. Recusar
    /// é o caso comum, não a exceção.
    /// </remarks>
    public static DocumentLink? TryCreate(string? url, string? label = null, bool wasWrapped = false)
    {
        var trimmed = url?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > URL_MAX_LENGTH)
            return null;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;

        if (string.IsNullOrEmpty(uri.Host))
            return null;

        var trimmedLabel = label?.Trim();
        if (trimmedLabel is { Length: > LABEL_MAX_LENGTH })
            trimmedLabel = trimmedLabel[..LABEL_MAX_LENGTH];

        return new DocumentLink(
            uri.AbsoluteUri,
            uri.Host.ToLowerInvariant(),
            uri.Port,
            uri.PathAndQuery.ToLowerInvariant(),
            string.IsNullOrEmpty(trimmedLabel) ? null : trimmedLabel,
            wasWrapped);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Url;
    }
}
