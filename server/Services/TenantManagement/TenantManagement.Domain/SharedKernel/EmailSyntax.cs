namespace TenantManagement.Domain.SharedKernel;

/// <summary>
/// A forma canônica de um endereço de e-mail e as checagens de sintaxe do BC.
/// </summary>
/// <remarks>
/// É helper estático, e não Value Object, porque os consumidores guardam a <em>string</em>
/// normalizada: o e-mail do membro é chave natural dentro do tenant e precisa ser
/// endereçável a partir da raiz para o índice único do EF.
/// </remarks>
public static class EmailSyntax
{
    /// <summary>Chave canônica de comparação: sem espaços nas pontas, em minúsculas.</summary>
    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

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
