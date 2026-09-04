namespace BillPayment.Domain.Secrets;

using System.Globalization;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Ponteiro para uma credencial guardada no cofre. <strong>Nunca o segredo.</strong>
/// </summary>
/// <remarks>
/// <para>
/// Este é o único formato de credencial que o Domain conhece. Um agregado guarda a referência
/// e não tem como, nem por descuido, guardar o valor: o VO não tem construtor que aceite um
/// segredo, e resolver a referência exige a porta <c>ISecretVault</c>, que vive na Infra.
/// </para>
/// <para>
/// <strong>O esquema no prefixo é intencional.</strong> <c>bpv1</c> identifica o cofre e a
/// versão do formato. Quando o Infisical entrar (gatilho no <c>ADR-009</c>), as referências
/// novas nascem com outro esquema e as antigas continuam resolvendo — e um <c>grep bpv1</c>
/// mostra exatamente o que ainda falta migrar. Uma referência opaca não daria isso.
/// </para>
/// </remarks>
public sealed class CredentialRef : ValueObject
{
    /// <summary>Cofre próprio (envelope encryption no Postgres), formato 1.</summary>
    public const string LOCAL_VAULT_SCHEME = "bpv1";

    public const int MAX_LENGTH = 100;

    private const char SEPARATOR = ':';

    public string Scheme { get; private set; } = string.Empty;

    /// <summary>Identificador da credencial dentro do cofre daquele esquema.</summary>
    public string Key { get; private set; } = string.Empty;

    private CredentialRef() { }

    /// <summary>Compõe a referência de uma credencial do cofre local.</summary>
    public static CredentialRef ForLocalVault(Guid id)
        => id == Guid.Empty
            ? throw SecretErrors.CredentialRefRequired()
            : new CredentialRef
            {
                Scheme = LOCAL_VAULT_SCHEME,
                Key = id.ToString("N", CultureInfo.InvariantCulture),
            };

    public static CredentialRef Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw SecretErrors.CredentialRefRequired();

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            throw SecretErrors.CredentialRefMalformed();

        var separator = trimmed.IndexOf(SEPARATOR, StringComparison.Ordinal);
        if (separator <= 0 || separator == trimmed.Length - 1)
            throw SecretErrors.CredentialRefMalformed();

        var scheme = trimmed[..separator];
        var key = trimmed[(separator + 1)..];

        if (key.Contains(SEPARATOR, StringComparison.Ordinal) || key.Any(char.IsWhiteSpace))
            throw SecretErrors.CredentialRefMalformed();

        return new CredentialRef { Scheme = scheme, Key = key };
    }

    public static bool TryParse(string? value, out CredentialRef? credentialRef)
    {
        credentialRef = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            credentialRef = Parse(value);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    public bool IsLocalVault => string.Equals(Scheme, LOCAL_VAULT_SCHEME, StringComparison.Ordinal);

    /// <summary>A chave como <see cref="Guid"/>, quando a referência é do cofre local.</summary>
    public Guid AsLocalVaultId()
        => IsLocalVault && Guid.TryParseExact(Key, "N", out var id)
            ? id
            : throw SecretErrors.CredentialRefMalformed();

    /// <summary>Forma canônica, que é o que vai para a coluna do banco.</summary>
    public override string ToString() => $"{Scheme}{SEPARATOR}{Key}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Scheme;
        yield return Key;
    }
}
