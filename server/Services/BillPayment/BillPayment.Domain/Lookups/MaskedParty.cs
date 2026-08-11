namespace BillPayment.Domain.Lookups;

using System.Text;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O pagador como o provedor devolve no decode do Pix: com o documento <strong>mascarado</strong>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Isto não revoga o ADR-004.</strong> Máscara não identifica ninguém e não serve para
/// atribuir um documento a um tenant. Serve para o que o ADR-004 já autoriza: <em>contradizer</em>.
/// Se os dígitos visíveis não podem pertencer ao <c>PayerProfile</c> do tenant, isso é
/// contradição, e contradição bloqueia. O contrário nunca vale — máscara compatível não
/// confirma nada, porque milhões de documentos compartilham quatro dígitos.
/// </para>
/// <para>
/// <see cref="IsCompatibleWith"/> foi escrito para <strong>errar para o lado de não concluir</strong>.
/// Só devolve <c>false</c> quando a comparação é posição a posição e um dígito visível difere.
/// Máscara de comprimento diferente do documento é ausência de conclusão, não contradição: não
/// há garantia de que o provedor preserve o comprimento, e travar um pagamento legítimo por
/// causa de um formato de máscara é pior do que deixar o check inconclusivo.
/// </para>
/// </remarks>
public sealed class MaskedParty : ValueObject
{
    public const char MASK_CHAR = '*';
    public const int NAME_MAX_LENGTH = 200;

    private const string SEPARATORS = ".-/ ";

    public string? Name { get; private set; }

    /// <summary>Documento normalizado: só dígitos e <see cref="MASK_CHAR"/>, sem pontuação.</summary>
    public string? MaskedTaxId { get; private set; }

    private MaskedParty() { }

    public static MaskedParty Of(string? name, string? maskedTaxId)
    {
        var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (trimmedName is { Length: > NAME_MAX_LENGTH })
            trimmedName = trimmedName[..NAME_MAX_LENGTH];

        var normalized = NormalizeMask(maskedTaxId);

        // Máscara sem nenhum dígito à mostra não contradiz nada; guardá-la só criaria a
        // ilusão de que o pagador foi conferido.
        if (normalized is not null && !normalized.Any(char.IsAsciiDigit))
            throw LookupErrors.MaskedTaxIdWithoutVisibleDigits();

        return new MaskedParty { Name = trimmedName, MaskedTaxId = normalized };
    }

    /// <summary>Quantos dígitos o provedor deixou à mostra. Zero significa que nada pode ser concluído.</summary>
    public int VisibleDigitCount => MaskedTaxId?.Count(char.IsAsciiDigit) ?? 0;

    /// <summary>
    /// O documento do tenant pode ser este pagador? <c>false</c> é uma contradição comprovada.
    /// </summary>
    public bool IsCompatibleWith(TaxId taxId)
    {
        if (taxId is null || MaskedTaxId is null || MaskedTaxId.Length != taxId.Value.Length)
            return true;

        for (var i = 0; i < MaskedTaxId.Length; i++)
        {
            if (MaskedTaxId[i] != MASK_CHAR && MaskedTaxId[i] != taxId.Value[i])
                return false;
        }

        return true;
    }

    /// <summary>Compatível com <strong>algum</strong> dos documentos do tenant — o principal ou os adicionais.</summary>
    public bool IsCompatibleWithAny(IEnumerable<TaxId> taxIds)
        => taxIds is null || !taxIds.Any() || taxIds.Any(IsCompatibleWith);

    // Qualquer caractere que não seja dígito nem separador é máscara, venha ele como '*', 'X' ou '•'.
    private static string? NormalizeMask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (SEPARATORS.Contains(c, StringComparison.Ordinal))
                continue;

            builder.Append(char.IsAsciiDigit(c) ? c : MASK_CHAR);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return MaskedTaxId;
    }
}
