namespace BillPayment.Application.Queries.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class BillQueries(BillPaymentDbContext context) : IBillQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<BillPage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.Bills.AsNoTracking().Where(b => b.TenantId == tenant);

        // Keyset por CreatedAt: o Id é value-converted e o EF não traduz comparação de ordem
        // sobre ele. Mais recente primeiro — é a ordem em que a fila é trabalhada.
        if (CursorCodec.TryDecode(cursor, out var beforeCreatedAt))
            query = query.Where(b => b.CreatedAt < beforeCreatedAt);

        var rows = await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0 ? CursorCodec.Encode(rows[^1].CreatedAt) : null;

        return new BillPage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<BillDto?> GetAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        return bill is null ? null : ToDto(bill);
    }

    public async Task<BillDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        if (bill is null)
            return null;

        var beneficiary = bill.Rail == PaymentRail.Pix
            ? bill.PixLookup?.Receiver ?? bill.Lookup?.Beneficiary
            : bill.Lookup?.Beneficiary ?? bill.PixLookup?.Receiver;

        var barcode = bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.Barcode);

        return new BillDetailDto(
            bill.Id.Value,
            bill.Status.Name,
            bill.Kind.Name,
            bill.Rail.Name,
            beneficiary is null
                ? null
                : new BillPartyDto(beneficiary.Name, beneficiary.TradingName, beneficiary.TaxId?.Formatted()),
            bill.PayableAmount?.Amount,
            bill.Lookup?.OriginalAmount?.Amount,
            ToDateTime(bill.Lookup?.DueDate ?? bill.PixLookup?.DueDate) ?? barcode?.DigitableLine.DueDate,
            bill.Lookup?.BankCode?.Value
                ?? (barcode is not null && barcode.DigitableLine.Kind.CarriesBankCode
                    ? barcode.DigitableLine.BankCode.Value
                    : null),
            ToDateTime(bill.Lookup?.MinimumScheduleDate),
            bill.LastConsultedAt?.UtcDateTime,

            // Ordem estável pelo id do tipo: a tela lista as doze sempre na mesma sequência,
            // e a do catálogo é a ordem de leitura que o doc 03 pede.
            [.. bill.Checks
                .OrderBy(c => c.Type.Id)
                .Select(c => new BillCheckDto(
                    c.Type.Name,
                    c.Outcome.Name,
                    c.Severity.Name,
                    c.ReasonCode,
                    c.Evidence,
                    c.IsBlockingFailure,
                    c.EvaluatedAt))],

            bill.Approval is null
                ? null
                : new BillApprovalDto(
                    bill.Approval.DecidedBy.Value,
                    bill.Approval.Decision.Name,
                    bill.Approval.DecidedAt,
                    bill.Approval.Note),
            ToDateTime(bill.ScheduledFor),
            new BillOriginDto(
                bill.Origin.SourceKind.Name,
                bill.Origin.SourceId,
                bill.Origin.SenderAddress,
                bill.Origin.ReceivedAt),
            bill.CreatedAt);
    }

    private static DateTime? ToDateTime(DateOnly? date)
        => date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static BillDto ToDto(Bill bill)
    {
        var barcode = bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.Barcode);

        // Projeção read-only para montar a resposta — não decide nada de domínio.
        var amount = bill.Instruments
            .Select(i => i.DeclaredAmount)
            .FirstOrDefault(a => a is not null);

        return new BillDto(
            bill.Id.Value,
            bill.Status.Name,
            bill.Kind.Name,
            bill.Rail.Name,
            amount?.Amount,
            barcode?.DigitableLine.DueDate,
            barcode is not null && barcode.DigitableLine.Kind.CarriesBankCode
                ? barcode.DigitableLine.BankCode.Value
                : null,
            new BillOriginDto(
                bill.Origin.SourceKind.Name,
                bill.Origin.SourceId,
                bill.Origin.SenderAddress,
                bill.Origin.ReceivedAt),
            bill.CreatedAt);
    }
}
