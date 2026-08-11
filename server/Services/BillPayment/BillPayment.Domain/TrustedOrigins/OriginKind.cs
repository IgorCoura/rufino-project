namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Natureza da origem cadastrada. A resolução de uma mensagem casa primeiro por
/// <see cref="EmailAddress"/> (mais específico) e só depois por <see cref="EmailDomain"/>.
/// </summary>
public sealed class OriginKind : Enumeration
{
    public static readonly OriginKind EmailAddress = new(1, nameof(EmailAddress), requiresAtSign: true, matchPrecedence: 1);
    public static readonly OriginKind EmailDomain = new(2, nameof(EmailDomain), requiresAtSign: false, matchPrecedence: 2);
    public static readonly OriginKind WebDomain = new(3, nameof(WebDomain), requiresAtSign: false, matchPrecedence: 3);

    /// <summary>Se o valor cadastrado precisa conter '@' (endereço) ou não pode contê-lo (domínio).</summary>
    public bool RequiresAtSign { get; }

    /// <summary>Menor vence: um endereço exato sobrepõe o domínio ao resolver a origem de uma mensagem.</summary>
    public int MatchPrecedence { get; }

    private OriginKind(int id, string name, bool requiresAtSign, int matchPrecedence) : base(id, name)
    {
        RequiresAtSign = requiresAtSign;
        MatchPrecedence = matchPrecedence;
    }
}
