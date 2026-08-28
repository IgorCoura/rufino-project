namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.Retention;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureRetentionPolicyRepository : ICaptureRetentionPolicyRepository
{
    private readonly BillPaymentDbContext _context;

    public CaptureRetentionPolicyRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(CaptureRetentionPolicy policy, CancellationToken cancellationToken = default)
        => await _context.CaptureRetentionPolicies.AddAsync(policy, cancellationToken);

    public Task<CaptureRetentionPolicy?> GetAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
        => _context.CaptureRetentionPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

    /// <summary>
    /// As políticas ligadas. Varre a instalação inteira, como os demais workers — quem não ligou
    /// não aparece, e por isso a purga nunca roda por omissão.
    /// </summary>
    public async Task<IReadOnlyList<CaptureRetentionPolicy>> ListEnabledAsync(
        int limit,
        CancellationToken cancellationToken = default)
        => await _context.CaptureRetentionPolicies
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
