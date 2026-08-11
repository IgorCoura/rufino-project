namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PayerProfileRepository : IPayerProfileRepository
{
    private readonly BillPaymentDbContext _context;

    public PayerProfileRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(PayerProfile profile, CancellationToken cancellationToken = default)
        => await _context.PayerProfiles.AddAsync(profile, cancellationToken);

    public Task<PayerProfile?> GetByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        // Sem Include: coleção owned é carregada automaticamente pelo EF.
        => _context.PayerProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

    public Task<bool> ExistsForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        => _context.PayerProfiles
            .AsNoTracking()
            .AnyAsync(p => p.TenantId == tenantId, cancellationToken);
}
