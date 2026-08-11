namespace BillPayment.Application.Queries.TrustedOrigins;

using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class TrustedOriginQueries(BillPaymentDbContext context) : ITrustedOriginQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<TrustedOriginPage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.TrustedOrigins.AsNoTracking().Where(o => o.TenantId == tenant);

        // Keyset ascendente por (CreatedAt, Id) — o Id desempata, senão um lote gravado no mesmo
        // instante torna inalcançável tudo além da primeira página (ver CursorCodec).
        if (CursorCodec.TryDecode(cursor, out var afterCreatedAt, out var afterId))
        {
            var afterOriginId = TrustedOriginId.From(afterId);

            query = query.Where(o =>
                o.CreatedAt > afterCreatedAt || (o.CreatedAt == afterCreatedAt && o.Id > afterOriginId));
        }

        var rows = await query
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new TrustedOriginPage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<TrustedOriginDto?> GetAsync(
        Guid tenantId,
        Guid trustedOriginId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = TrustedOriginId.From(trustedOriginId);

        var origin = await context.TrustedOrigins
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == tenant && o.Id == id, cancellationToken);

        return origin is null ? null : ToDto(origin);
    }

    public async Task<TrustedOriginDto?> ResolveBySenderAsync(
        Guid tenantId,
        string senderAddress,
        CancellationToken cancellationToken = default)
    {
        var normalized = TrustedOrigin.Normalize(senderAddress);
        if (normalized.Length == 0)
            return null;

        var domain = TrustedOrigin.ExtractDomain(normalized);
        var tenant = TenantId.From(tenantId);

        var candidates = await context.TrustedOrigins
            .AsNoTracking()
            .Where(o => o.TenantId == tenant && (o.Value == normalized || o.Value == domain))
            .ToListAsync(cancellationToken);

        var match = candidates
            .Where(o => o.Matches(normalized))
            .OrderBy(o => o.Kind.MatchPrecedence)
            .FirstOrDefault();

        return match is null ? null : ToDto(match);
    }

    private static TrustedOriginDto ToDto(TrustedOrigin o)
        => new(o.Id.Value, o.Kind.Name, o.Value, o.Decision.Name, o.DecidedBy.Value, o.DecidedAt, o.Note);
}
