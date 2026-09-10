namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Uma senha candidata para abrir um PDF cifrado, junto do <strong>rótulo do campo que a
/// originou</strong>.
/// </summary>
/// <remarks>
/// <para>
/// O par existe porque as duas metades têm destinos opostos: <see cref="Value"/> vive só em
/// memória durante a tentativa e <strong>nunca</strong> é logado, gravado ou devolvido por API;
/// <see cref="DerivedFrom"/> é o que fica registrado no <c>CaptureItem.UnlockedBy</c> como
/// evidência auditável (ADR-009).
/// </para>
/// <para>
/// <strong>Isto é derivação, não força bruta.</strong> Os candidatos saem de dados que o próprio
/// tenant cadastrou — os N primeiros dígitos do CNPJ, o CPF, a data de nascimento do titular — e
/// o teto de tentativas por documento existe para que um PDF hostil não vire um laço caro.
/// </para>
/// <para>
/// <see cref="ToString"/> é sobrescrito de propósito: sem isso, uma interpolação distraída em
/// log ou mensagem de exceção imprimiria a senha.
/// </para>
/// </remarks>
public sealed class PasswordCandidate : ValueObject
{
    public const int DERIVED_FROM_MAX_LENGTH = 100;

    /// <summary>
    /// O menor prefixo de documento que ainda vale como <strong>prova forte</strong> de
    /// propriedade no degrau 0 do roteamento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Abaixo disto o espaço de valores fica pequeno o bastante para o acerto ser coincidência:
    /// três dígitos são mil possibilidades, e prefixos de CNPJ não se distribuem por igual. A
    /// senha continua sendo tentada — ela abre documentos reais —, mas "abriu com três dígitos"
    /// deixa de ser, sozinha, evidência de que o emissor endereçou o boleto a este tenant.
    /// </para>
    /// <para>
    /// O número vive aqui, e não no <c>BillRoutingService</c>, porque quem constrói o rótulo é
    /// este VO: a régua e o vocabulário que ela lê têm de sair do mesmo lugar.
    /// </para>
    /// </remarks>
    public const int STRONG_PREFIX_MIN_LENGTH = 5;

    private const string PREFIX_MARKER = "_first_";

    /// <summary>Senha vazia — cobre PDF com apenas <em>owner password</em>, o caso mais comum.</summary>
    public static readonly PasswordCandidate Empty = new(string.Empty, "empty");

    /// <summary>A senha em claro. Existe só durante a tentativa de abertura.</summary>
    public string Value { get; }

    /// <summary>
    /// Qual campo do cadastro derivou esta senha (<c>cnpj_first_5</c>, <c>cpf_full</c>,
    /// <c>birth_ddmmyyyy</c>, <c>learned_for_payee</c>). É isto que vira evidência.
    /// </summary>
    public string DerivedFrom { get; }

    private PasswordCandidate(string value, string derivedFrom)
    {
        Value = value;
        DerivedFrom = derivedFrom;
    }

    public static PasswordCandidate From(string? value, string derivedFrom)
    {
        var label = derivedFrom?.Trim();
        if (string.IsNullOrEmpty(label))
            throw ExtractionErrors.PasswordLabelRequired();

        if (label.Length > DERIVED_FROM_MAX_LENGTH)
            label = label[..DERIVED_FROM_MAX_LENGTH];

        return new PasswordCandidate(value ?? string.Empty, label);
    }

    /// <summary>
    /// Monta o rótulo de uma senha derivada de documento fiscal — <c>cnpj_first_5_primary</c>,
    /// <c>cpf_full_additional_1</c>.
    /// </summary>
    /// <remarks>
    /// O formato vive aqui, e não em quem deriva, porque <see cref="IsShortPrefixProof"/> o lê de
    /// volta: produtor e leitor do mesmo vocabulário separados por dois arquivos é como um ganha
    /// um sufixo novo e o outro passa a não reconhecer nada.
    /// </remarks>
    public static string LabelForDocument(
        string documentKind, int prefixLength, int documentLength, string origin)
        => prefixLength >= documentLength
            ? $"{documentKind}_full_{origin}"
            : $"{documentKind}{PREFIX_MARKER}{prefixLength}_{origin}";

    /// <summary>
    /// O rótulo descreve um prefixo <strong>curto demais</strong> para sustentar sozinho a prova
    /// de propriedade do degrau 0?
    /// </summary>
    /// <remarks>
    /// Documento completo nunca é curto, e rótulo que não descreve prefixo (a senha vazia, ou um
    /// formato que ainda não existe) responde <c>false</c>: a régua só rebaixa o que ela
    /// reconhece, e tratar o desconhecido como fraco rebaixaria todo rótulo futuro por omissão.
    /// </remarks>
    public static bool IsShortPrefixProof(string? derivedFrom)
    {
        if (string.IsNullOrEmpty(derivedFrom))
            return false;

        var marker = derivedFrom.IndexOf(PREFIX_MARKER, StringComparison.Ordinal);
        if (marker < 0)
            return false;

        var digits = derivedFrom.AsSpan(marker + PREFIX_MARKER.Length);
        var end = digits.IndexOf('_');
        if (end >= 0)
            digits = digits[..end];

        return int.TryParse(digits, out var length) && length < STRONG_PREFIX_MIN_LENGTH;
    }

    /// <summary>Só o rótulo. A senha nunca aparece em texto — nem por acidente.</summary>
    public override string ToString() => $"PasswordCandidate({DerivedFrom})";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return DerivedFrom;
    }
}
