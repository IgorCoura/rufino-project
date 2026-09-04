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

    /// <summary>Só o rótulo. A senha nunca aparece em texto — nem por acidente.</summary>
    public override string ToString() => $"PasswordCandidate({DerivedFrom})";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return DerivedFrom;
    }
}
