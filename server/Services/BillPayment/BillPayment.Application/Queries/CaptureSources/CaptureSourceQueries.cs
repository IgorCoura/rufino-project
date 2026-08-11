namespace BillPayment.Application.Queries.CaptureSources;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureSourceQueries(BillPaymentDbContext context) : ICaptureSourceQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<CaptureSourcePage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.CaptureSources.AsNoTracking().Where(s => s.TenantId == tenant);

        // Keyset por CreatedAt, não por Id: o Id é value-converted e o EF não traduz comparação
        // de ordem sobre ele. CreatedAt é DateTime e traduz direto.
        if (CursorCodec.TryDecode(cursor, out var afterCreatedAt))
            query = query.Where(s => s.CreatedAt > afterCreatedAt);

        var rows = await query
            .OrderBy(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0 ? CursorCodec.Encode(rows[^1].CreatedAt) : null;

        return new CaptureSourcePage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<CaptureSourceDto?> GetAsync(
        Guid tenantId,
        Guid captureSourceId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = CaptureSourceId.From(captureSourceId);

        var source = await context.CaptureSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenant && s.Id == id, cancellationToken);

        return source is null ? null : ToDto(source);
    }

    // O ponteiro do cofre nunca sai daqui — só a informação de que existe uma credencial.
    private static CaptureSourceDto ToDto(CaptureSource s)
        => new(
            s.Id.Value,
            s.Kind.Name,
            s.DisplayName,
            s.Address,
            s.FolderPath,
            s.Credential is not null,
            s.IsEnabled,
            s.LastSyncAt,
            s.LastSyncError,
            !string.IsNullOrEmpty(s.SyncCursor),
            s.CreatedAt);
}
