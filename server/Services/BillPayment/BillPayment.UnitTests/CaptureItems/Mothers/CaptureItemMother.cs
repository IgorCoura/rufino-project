namespace BillPayment.UnitTests.CaptureItems.Mothers;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;

internal static class CaptureItemMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 8, 10, 9, 30, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultReceivedAt = new(2026, 8, 10, 8, 15, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly CaptureSourceId DefaultSource =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));
    public static readonly BillId DefaultBill = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d1"));
    public static readonly UserId DefaultUser = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));

    public const string DefaultMessageId = "AAMkAGI2THVSAAA=";
    public const string DefaultArtifactKey = "boleto-enel-092026.pdf";
    public const string DefaultSender = "faturas@enel.com.br";
    public const string DefaultContentHash = "sha256:9f2c4a1b";
    public const string DefaultStorageKey = "tenants/0195a1f0/capture/boleto-enel-092026.pdf";

    public static CaptureItem Ingest(
        string? externalMessageId = null,
        string? artifactKey = null,
        string? sender = null,
        string? subject = "Sua fatura de energia chegou",
        DateTime? receivedAt = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null,
        CaptureSourceId? sourceId = null)
        => CaptureItem.Ingest(
            tenantId ?? DefaultTenant,
            sourceId ?? DefaultSource,
            externalMessageId ?? DefaultMessageId,
            artifactKey ?? DefaultArtifactKey,
            sender ?? DefaultSender,
            subject,
            receivedAt ?? DefaultReceivedAt,
            occurredAt ?? DefaultOccurredAt);

    /// <summary>Item com o artefato já armazenado — pré-requisito de qualquer processamento.</summary>
    public static CaptureItem Stored()
    {
        var item = Ingest();
        item.StoreArtifact(DefaultContentHash, DefaultStorageKey, DefaultOccurredAt);
        return item;
    }

    /// <summary>Item com instrumento extraído, pronto para o roteamento.</summary>
    public static CaptureItem Parsed()
    {
        var item = Stored();
        item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, DefaultOccurredAt);
        return item;
    }

    /// <summary>Item na fila de reivindicação do dono da fonte.</summary>
    public static CaptureItem Unrouted()
    {
        var item = Parsed();
        item.MarkUnrouted("no_rule_matched", DefaultOccurredAt);
        return item;
    }

    /// <summary>Item cujo pagador foi identificado e não é deste tenant.</summary>
    public static CaptureItem Foreign()
    {
        var item = Parsed();
        item.MarkForeign("payer_belongs_to_other_tenant", DefaultOccurredAt);
        return item;
    }
}
