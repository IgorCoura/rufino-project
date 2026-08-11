namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class TrustedOriginRepository : ITrustedOriginRepository
{
    private readonly BillPaymentDbContext _context;

    public TrustedOriginRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(TrustedOrigin origin, CancellationToken cancellationToken = default)
        => await _context.TrustedOrigins.AddAsync(origin, cancellationToken);

    public Task<TrustedOrigin?> GetAsync(
        TenantId tenantId,
        TrustedOriginId id,
        CancellationToken cancellationToken = default)
        => _context.TrustedOrigins
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        OriginKind kind,
        string normalizedValue,
        CancellationToken cancellationToken = default)
        => _context.TrustedOrigins
            .AsNoTracking()
            .AnyAsync(
                o => o.TenantId == tenantId && o.Kind == kind && o.Value == normalizedValue,
                cancellationToken);

    public async Task<TrustedOrigin?> ResolveBySenderAsync(
        TenantId tenantId,
        string senderAddress,
        CancellationToken cancellationToken = default)
    {
        // Normaliza aqui, no C#, e não no SQL: a chave canônica é a do domínio
        // (TrustedOrigin.Normalize) e reimplementá-la em LINQ traduzido seria a forma
        // mais fácil de a resolução divergir do que foi gravado.
        var normalized = TrustedOrigin.Normalize(senderAddress);
        if (normalized.Length == 0)
            return null;

        var domain = TrustedOrigin.ExtractDomain(normalized);

        var candidates = await _context.TrustedOrigins
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && (o.Value == normalized || o.Value == domain))
            .ToListAsync(cancellationToken);

        // Endereço exato vence domínio — a ordem está no Smart Enum, não espalhada aqui.
        return candidates
            .Where(o => o.Matches(normalized))
            .OrderBy(o => o.Kind.MatchPrecedence)
            .FirstOrDefault();
    }

    public void Remove(TrustedOrigin origin) => _context.TrustedOrigins.Remove(origin);
}
