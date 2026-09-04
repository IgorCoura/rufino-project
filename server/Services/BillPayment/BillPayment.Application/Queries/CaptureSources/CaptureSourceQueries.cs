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

        // Keyset ascendente por (CreatedAt, Id) — o Id desempata, senão um lote gravado no mesmo
        // instante torna inalcançável tudo além da primeira página (ver CursorCodec).
        if (CursorCodec.TryDecode(cursor, out var afterCreatedAt, out var afterId))
        {
            var afterSourceId = CaptureSourceId.From(afterId);

            query = query.Where(s =>
                s.CreatedAt > afterCreatedAt || (s.CreatedAt == afterCreatedAt && s.Id > afterSourceId));
        }

        var rows = await query
            .OrderBy(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

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

    // Nem o ponteiro do cofre nem o cursor das pastas saem daqui — só a informação de que existem.
    private static CaptureSourceDto ToDto(CaptureSource s)
        => new(
            s.Id.Value,
            s.Kind.Name,
            s.DisplayName,
            s.Address,
            [.. s.Folders.OrderBy(f => f.Path ?? string.Empty, StringComparer.OrdinalIgnoreCase).Select(ToDto)],
            s.Credential is not null,
            s.IsEnabled,
            s.CaptureSince,
            s.LastSyncAt,
            s.LastSyncError,
            s.CreatedAt);

    private static MonitoredFolderDto ToDto(MonitoredFolder f)
        => new(
            f.Id.Value,
            f.Path,
            !string.IsNullOrEmpty(f.SyncCursor),
            f.LastSyncAt,
            f.LastSyncError);
}
