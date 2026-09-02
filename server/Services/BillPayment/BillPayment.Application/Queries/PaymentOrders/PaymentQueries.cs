namespace BillPayment.Application.Queries.PaymentOrders;

using BillPayment.Domain.Bills;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PaymentQueries(
    BillPaymentDbContext context,
    Domain.Ports.IAttachmentStorage storage) : IPaymentQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<PaymentOrderPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.PaymentOrders.AsNoTracking().Where(o => o.TenantId == tenant);

        // Mesmo contrato do filtro das outras listas: casa pelo nome, caixa-insensível, e valor
        // desconhecido devolve tudo — engano de cliente não é 500 nem lista vazia enganosa.
        if (TryParseStatus(status, out var parsed))
            query = query.Where(o => o.Status == parsed);

        // Keyset descendente por (CreatedAt, Id), desempate na MESMA direção da chave.
        if (CursorCodec.TryDecode(cursor, out var beforeCreatedAt, out var beforeId))
        {
            var beforeOrderId = PaymentOrderId.From(beforeId);

            query = query.Where(o =>
                o.CreatedAt < beforeCreatedAt || (o.CreatedAt == beforeCreatedAt && o.Id < beforeOrderId));
        }

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new PaymentOrderPage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<PaymentOrderDto?> GetAsync(
        Guid tenantId,
        Guid paymentOrderId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = PaymentOrderId.From(paymentOrderId);

        var order = await context.PaymentOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == tenant && o.Id == id, cancellationToken);

        return order is null ? null : ToDto(order);
    }

    public async Task<PaymentOrderDto?> GetByBillAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var bill = BillId.From(billId);

        // A mais recente vence: um boleto reaberto tem a ordem falhada E a nova, e o detalhe
        // fala da atual — a história completa é a lista filtrada pelo boleto, quando existir.
        var order = await context.PaymentOrders
            .AsNoTracking()
            .Where(o => o.TenantId == tenant && o.BillId == bill)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? null : ToDto(order);
    }

    public async Task<ArtifactDownload?> GetReceiptAsync(
        Guid tenantId,
        Guid paymentOrderId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = PaymentOrderId.From(paymentOrderId);

        var receipt = await context.PaymentOrders
            .AsNoTracking()
            .Where(o => o.TenantId == tenant && o.Id == id)
            .Select(o => new { o.ReceiptStorageKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (receipt is null || string.IsNullOrEmpty(receipt.ReceiptStorageKey))
            return null;

        // Comprovante não é cifrado — vem do provedor, não da caixa — então serve direto do
        // fluxo do balde, sem passar pelo UnlockedArtifactReader.
        var artifact = await storage.OpenAsync(tenant, receipt.ReceiptStorageKey, cancellationToken);
        return artifact is null
            ? null
            : ArtifactDownload.From(artifact, null, $"comprovante-{paymentOrderId}");
    }

    private static PaymentOrderDto ToDto(PaymentOrder order)
        => new(
            order.Id.Value,
            order.BillId.Value,
            order.Rail.Name,
            order.Status.Name,
            order.Hold.Name,
            order.RequestedScheduleDate,
            order.EffectiveScheduleDate,
            order.Amount?.Amount,
            order.Fee?.Amount,
            order.PaidAt,
            order.FailReasons,
            order.LastError,
            order.SubmissionAttempts,
            RequiresConfirmation: order.Hold == PaymentOrderHold.AwaitingConfirmation,
            order.ConfirmedBy?.Value,
            HasReceipt: !string.IsNullOrEmpty(order.ReceiptStorageKey),
            order.LastProviderSyncAt,
            order.CreatedAt,
            order.UpdatedAt);

    private static bool TryParseStatus(string? status, out PaymentOrderStatus parsed)
    {
        parsed = default!;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var match = Enumeration.GetAll<PaymentOrderStatus>()
            .FirstOrDefault(s => string.Equals(s.Name, status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return false;

        parsed = match;
        return true;
    }
}
