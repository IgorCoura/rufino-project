namespace BillPayment.Domain.TrustedOrigins;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Decisão explícita do tenant sobre uma origem. A ausência de registro significa
/// origem <em>desconhecida</em> — que não é nem confiável nem hostil, e por isso
/// não é representada aqui.
/// </summary>
public sealed class TrustDecision : Enumeration
{
    public static readonly TrustDecision Trusted = new(1, nameof(Trusted));
    public static readonly TrustDecision Blocked = new(2, nameof(Blocked));

    private TrustDecision(int id, string name) : base(id, name) { }
}
