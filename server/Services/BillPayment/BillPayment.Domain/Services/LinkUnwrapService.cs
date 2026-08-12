namespace BillPayment.Domain.Services;

/// <summary>
/// Reduz um endereço embrulhado em rastreador de campanha ao endereço que seria de fato visitado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Desembrulhar é decisão de segurança, não de estética.</strong> Todo boleto por link
/// medido na caixa real (2026-08-11) chega dentro de um rastreador — a SABESP e a Perfil Líder por
/// <c>awstrack.me/L0/…</c>, a EDP por <c>tracking.edpbr.com.br</c>. Uma allowlist aplicada ao
/// <c>href</c> cru estaria decidindo sobre o domínio do <em>rastreador</em>, que é o mesmo para o
/// boleto legítimo e para qualquer coisa que um remetente resolva embrulhar. Autorizar o rastreador
/// é autorizar redirecionamento para qualquer lugar.
/// </para>
/// <para>
/// <strong>Sem rede.</strong> O endereço de verdade vem percent-encoded dentro do próprio caminho
/// ou da própria consulta, então decodificar basta — e é estritamente melhor que seguir o
/// redirecionamento: não gasta chamada, não pode ser enganado por um <c>Location</c> diferente do
/// anunciado, e não entrega ao remetente a confirmação de que a mensagem foi aberta.
/// </para>
/// <para>
/// <strong>O que não desembrulha continua como está.</strong> A EDP embrulha num identificador
/// opaco (<c>?ref=0ygAA…</c>) que não carrega URL nenhuma; ali não há o que decodificar, e o link
/// segue apontando para o rastreador — onde a allowlist o recusa, que é o desfecho correto.
/// </para>
/// </remarks>
public static class LinkUnwrapService
{
    /// <summary>
    /// Quantas camadas de embrulho são desfeitas. Rastreador dentro de rastreador acontece quando
    /// um e-mail é reencaminhado por outra plataforma; três camadas cobrem o que já se viu e
    /// impede que um endereço construído de propósito vire laço.
    /// </summary>
    private const int MAX_DEPTH = 3;

    /// <summary>
    /// Nomes de parâmetro que carregam o destino em rastreadores por query string.
    /// </summary>
    private static readonly string[] RedirectParameters =
        ["url", "u", "q", "target", "redirect", "redirect_url", "redirecturl", "link", "dest", "destination"];

    /// <summary>
    /// Devolve o endereço final e se houve embrulho a desfazer.
    /// </summary>
    /// <remarks>
    /// Entrada que não é URL absoluta http(s) volta inalterada — quem recusa é o
    /// <c>DocumentLink</c>, num lugar só.
    /// </remarks>
    public static (string Url, bool WasWrapped) Unwrap(string? url)
    {
        var current = url?.Trim();

        if (string.IsNullOrEmpty(current) || !IsAbsoluteHttp(current, out _))
            return (current ?? string.Empty, false);

        var wrapped = false;

        for (var depth = 0; depth < MAX_DEPTH; depth++)
        {
            var inner = ExtractEmbeddedUrl(current);

            if (inner is null || string.Equals(inner, current, StringComparison.Ordinal))
                break;

            current = inner;
            wrapped = true;
        }

        return (current, wrapped);
    }

    /// <summary>
    /// Procura uma URL absoluta escondida dentro do caminho ou da consulta.
    /// </summary>
    /// <remarks>
    /// O caminho vem primeiro porque é a forma do rastreador do SES, que é a medida na caixa real:
    /// <c>/L0/https:%2F%2Fdestino…/1/…</c> — o destino é um segmento inteiro, percent-encoded.
    /// </remarks>
    private static string? ExtractEmbeddedUrl(string url)
    {
        if (!IsAbsoluteHttp(url, out var uri))
            return null;

        foreach (var segment in uri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Decode(segment);

            if (candidate is not null && IsAbsoluteHttp(candidate, out _))
                return candidate;
        }

        return FromQuery(uri.Query);
    }

    private static string? FromQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return null;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
                continue;

            var name = pair[..separator];

            if (!Array.Exists(RedirectParameters, p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var candidate = Decode(pair[(separator + 1)..]);

            if (candidate is not null && IsAbsoluteHttp(candidate, out _))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Decodifica sem deixar entrada malformada escapar como exceção — percent-encoding quebrado
    /// é comum em e-mail que passou por reencaminhamento, e não pode derrubar a varredura.
    /// </summary>
    private static string? Decode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static bool IsAbsoluteHttp(string value, out Uri? uri)
        => Uri.TryCreate(value, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
