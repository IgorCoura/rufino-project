namespace BillPayment.UnitTests.TrustedOrigins.Mothers;

using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;

internal static class TrustedOriginMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly UserId DefaultDecidedBy = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));

    public const string DefaultAddress = "financeiro@fornecedor.com.br";
    public const string DefaultDomain = "fornecedor.com.br";

    /// <summary>Caminho feliz: omitir um parâmetro aplica o default do cenário.</summary>
    public static TrustedOrigin Register(
        OriginKind? kind = null,
        string? value = null,
        TrustDecision? decision = null,
        UserId? decidedBy = null,
        string? note = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => RegisterVerbatim(
            kind ?? OriginKind.EmailAddress,
            value ?? DefaultAddress,
            decision ?? TrustDecision.Trusted,
            decidedBy ?? DefaultDecidedBy,
            note,
            occurredAt,
            tenantId);

    /// <summary>
    /// Repassa <c>kind</c> e <c>decision</c> sem coalescer — é o único caminho capaz de
    /// exercitar as invariantes que rejeitam esses argumentos nulos. <see cref="Register"/>
    /// substituiria o nulo pelo default e o teste passaria a não testar nada.
    /// </summary>
    public static TrustedOrigin RegisterVerbatim(
        OriginKind kind,
        string value,
        TrustDecision decision,
        UserId decidedBy,
        string? note = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => TrustedOrigin.Register(
            tenantId ?? DefaultTenant,
            kind,
            value,
            decision,
            decidedBy,
            note,
            occurredAt ?? DefaultOccurredAt);

    public static TrustedOrigin TrustedAddress(string value = DefaultAddress)
        => Register(OriginKind.EmailAddress, value, TrustDecision.Trusted);

    public static TrustedOrigin TrustedDomain(string value = DefaultDomain)
        => Register(OriginKind.EmailDomain, value, TrustDecision.Trusted);

    public static TrustedOrigin BlockedAddress(string value = DefaultAddress)
        => Register(OriginKind.EmailAddress, value, TrustDecision.Blocked);
}
