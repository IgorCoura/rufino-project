namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.Notifications;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class TenantNotificationSettingsRepository : ITenantNotificationSettingsRepository
{
    private readonly BillPaymentDbContext _context;

    public TenantNotificationSettingsRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(
        TenantNotificationSettings settings, CancellationToken cancellationToken = default)
        => await _context.TenantNotificationSettings.AddAsync(settings, cancellationToken);

    public Task<TenantNotificationSettings?> GetAsync(
        TenantId tenantId, CancellationToken cancellationToken = default)
        => _context.TenantNotificationSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public Task<TenantNotificationSettings?> FindForDeliveryAsync(
        TenantId tenantId, CancellationToken cancellationToken = default)
        => _context.TenantNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
}
