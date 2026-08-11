namespace BillPayment.Domain.SharedKernel;

/// <summary>
/// A forma canônica de um endereço de e-mail e as duas checagens de sintaxe que o BC aplica.
/// </summary>
/// <remarks>
/// <para>
/// É helper estático, e não Value Object, porque os consumidores guardam a <em>string</em>
/// normalizada — <c>TrustedOrigin.Value</c> precisa ser endereçável a partir da raiz para o
/// índice único do EF, e o mesmo vale para o endereço da caixa monitorada. Um VO aqui viraria
/// owned type e reintroduziria o problema documentado no CLAUDE.md.
/// </para>
/// <para>
/// <strong>A validação é deliberadamente frouxa.</strong> Não é RFC 5322: aceita o que um
/// remetente real usa e rejeita o que claramente não é endereço. Endereço de origem chega de
/// fora, e recusar um remetente legítimo por rigor sintático perderia o boleto — que é pior do
/// que aceitar um exótico, já que nada aqui autentica ninguém.
/// </para>
/// </remarks>
public static class EmailSyntax
{
    /// <summary>Chave canônica de comparação: sem espaços nas pontas, em minúsculas.</summary>
    /// <remarks>
    /// Normalizar em dois lugares diferentes é como a resolução passa a divergir do que foi
    /// cadastrado — todo consumidor entra por aqui, inclusive a Application ao montar busca.
    /// </remarks>
    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

    /// <summary>Extrai o domínio de um endereço já normalizado. Vazio quando não há '@'.</summary>
    public static string ExtractDomain(string? emailAddress)
    {
        var normalized = Normalize(emailAddress);
        var at = normalized.LastIndexOf('@');
        return at < 0 || at == normalized.Length - 1 ? string.Empty : normalized[(at + 1)..];
    }

    /// <summary>Verifica a sintaxe de um endereço completo. O valor precisa vir normalizado.</summary>
    public static bool IsValidAddress(string normalized)
    {
        ArgumentNullException.ThrowIfNull(normalized);

        var at = normalized.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != normalized.LastIndexOf('@'))
            return false;

        var local = normalized[..at];
        var domain = normalized[(at + 1)..];
        return !local.Contains(' ', StringComparison.Ordinal) && IsValidDomain(domain);
    }

    /// <summary>Verifica a sintaxe de um domínio. O valor precisa vir normalizado.</summary>
    public static bool IsValidDomain(string normalized)
    {
        ArgumentNullException.ThrowIfNull(normalized);

        if (normalized.Contains('@', StringComparison.Ordinal) || normalized.Contains(' ', StringComparison.Ordinal))
            return false;
        if (normalized.StartsWith('.') || normalized.EndsWith('.') || normalized.Contains("..", StringComparison.Ordinal))
            return false;

        var labels = normalized.Split('.');
        if (labels.Length < 2)
            return false;

        foreach (var label in labels)
        {
            if (label.Length == 0 || label.StartsWith('-') || label.EndsWith('-'))
                return false;
            if (!label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                return false;
        }

        return true;
    }
}
