namespace BillPayment.UnitTests.CaptureSources.Mothers;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

internal static class CaptureSourceMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly CredentialRef DefaultCredential =
        CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));

    public const string DefaultMailbox = "contas@empresa.com.br";
    public const string DefaultPortalUrl = "https://portal.concessionaria.com.br/AreaCliente";

    public static CaptureSource Connect(
        CaptureSourceKind? kind = null,
        string? displayName = null,
        string? address = null,
        CredentialRef? credential = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => ConnectVerbatim(
            kind ?? CaptureSourceKind.MicrosoftGraphMailbox,
            displayName ?? "Caixa de contas a pagar",
            address ?? DefaultMailbox,
            credential ?? DefaultCredential,
            occurredAt,
            tenantId);

    /// <summary>
    /// Repassa <c>kind</c> e <c>credential</c> sem coalescer — é o único caminho capaz de
    /// exercitar as invariantes que rejeitam esses argumentos nulos. <see cref="Connect"/>
    /// substituiria o nulo pelo default e o teste passaria a não testar nada.
    /// </summary>
    public static CaptureSource ConnectVerbatim(
        CaptureSourceKind kind,
        string displayName,
        string address,
        CredentialRef? credential,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => CaptureSource.Connect(
            tenantId ?? DefaultTenant,
            kind,
            displayName,
            address,
            credential,
            occurredAt ?? DefaultOccurredAt);

    /// <summary>Fonte já sincronizada uma vez, com cursor no lugar.</summary>
    public static CaptureSource Synced(string cursor = "deltaLink-abc123")
    {
        var source = Connect();
        source.RecordSyncSuccess(cursor, DefaultOccurredAt.AddMinutes(5));
        return source;
    }
}
