namespace BillPayment.UnitTests.Bills.Mothers;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Instruments;

internal static class BillMother
{
    public static readonly DateTime DefaultOccurredAt = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DefaultTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    public static readonly Guid DefaultSourceId = new("0195a1f0-0000-7000-8000-0000000000d1");

    public const string DefaultSender = "financeiro@fornecedor.com.br";

    public static BillOrigin MailboxOrigin(string? sender = null)
        => BillOrigin.Create(
            BillSourceKind.Mailbox,
            DefaultOccurredAt,
            sourceId: DefaultSourceId,
            senderAddress: sender ?? DefaultSender,
            externalMessageId: "AAMkAGI2-fake-graph-id");

    public static BillOrigin ManualOrigin()
        => BillOrigin.Create(
            BillSourceKind.ManualUpload,
            DefaultOccurredAt,
            storageKey: "tenant-1/2026-07/boleto.pdf");

    /// <summary>Caminho feliz: omitir um parâmetro aplica o default do cenário (boleto por e-mail).</summary>
    public static Bill Capture(
        IReadOnlyCollection<PaymentInstrument>? instruments = null,
        BillOrigin? origin = null,
        DateTime? occurredAt = null,
        TenantId? tenantId = null)
        => CaptureVerbatim(
            instruments ?? [InstrumentSamples.Barcode()],
            origin ?? MailboxOrigin(),
            occurredAt,
            tenantId);

    /// <summary>
    /// Repassa os argumentos sem coalescer — único caminho capaz de exercitar as invariantes
    /// que rejeitam nulo ou coleção vazia. <see cref="Capture"/> substituiria pelo default.
    /// </summary>
    public static Bill CaptureVerbatim(
        IReadOnlyCollection<PaymentInstrument> instruments,
        BillOrigin origin,
        DateTime? occurredAt = null,
        TenantId? tenantId = null,
        PartyInfo? extractedPayer = null,
        RoutingConfidence? routing = null)
        => Bill.Capture(
            tenantId ?? DefaultTenant,
            instruments,
            origin,
            occurredAt ?? DefaultOccurredAt,
            extractedPayer,
            routing);

    public static Bill WithBothRails()
        => Capture([InstrumentSamples.Barcode(), InstrumentSamples.DynamicPixQr()]);

    public static Bill StaticPixOnly()
        => Capture([InstrumentSamples.StaticPix()]);
}
