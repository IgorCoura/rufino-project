namespace BillPayment.UnitTests.CapturedMessages.Mothers;

using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;

internal static class CapturedMessageMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 8, 19, 9, 30, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultReceivedAt = new(2026, 8, 19, 8, 15, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly CaptureSourceId DefaultSource =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));

    public const string DefaultMessageId = "AAMkAGI2THVSAAA=";
    public const string DefaultInternetMessageId = "<abc123@fornecedor.com.br>";
    public const string DefaultSender = "faturas@enel.com.br";
    public const string BoletoKey = "anexo-boleto";
    public const string ReciboKey = "anexo-recibo";

    public static CapturedMessage Register(
        string? externalMessageId = null,
        string? internetMessageId = DefaultInternetMessageId,
        string? sender = null,
        string? subject = "Sua fatura de energia chegou",
        DateTime? receivedAt = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null,
        params (string Key, string? FileName, string? ContentType)[] artifacts)
        => CapturedMessage.Register(
            tenantId ?? DefaultTenant,
            DefaultSource,
            externalMessageId ?? DefaultMessageId,
            sender ?? DefaultSender,
            subject,
            receivedAt ?? DefaultReceivedAt,
            occurredAt ?? DefaultOccurredAt,
            artifacts.Length > 0 ? artifacts : [(BoletoKey, "boleto.pdf", "application/pdf")],
            internetMessageId);

    /// <summary>Um e-mail com dois anexos — o caso real que tem desfechos diferentes na mesma mensagem.</summary>
    public static CapturedMessage WithTwoArtifacts()
        => Register(artifacts:
        [
            (BoletoKey, "boleto.pdf", "application/pdf"),
            (ReciboKey, "recibo.pdf", "application/pdf"),
        ]);
}
