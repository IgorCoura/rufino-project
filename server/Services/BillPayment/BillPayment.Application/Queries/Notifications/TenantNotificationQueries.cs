namespace BillPayment.Application.Queries.Notifications;

using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
internal sealed class TenantNotificationQueries(BillPaymentDbContext context)
    : ITenantNotificationQueries
{
    public async Task<TenantNotificationSettingsDto> GetAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await context.TenantNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId.From(tenantId), cancellationToken);

        return settings is null
            ? new TenantNotificationSettingsDto([], false)
            : new TenantNotificationSettingsDto([.. settings.Recipients], settings.IsEnabled);
    }
}
