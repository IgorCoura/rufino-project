namespace BillPayment.Application.Queries.PayerProfiles;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PayerProfileQueries(BillPaymentDbContext context) : IPayerProfileQueries
{
    public async Task<PayerProfileDto?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);

        var profile = await context.PayerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant, cancellationToken);

        return profile is null ? null : ToDto(profile);
    }

    private static PayerProfileDto ToDto(PayerProfile p)
        => new(
            p.Id.Value,
            p.Kind.Name,
            p.LegalName,
            p.PrimaryTaxId.Value,
            p.PrimaryTaxId.Kind.Name,
            p.AdditionalTaxIds.Select(t => new PayerProfileTaxIdDto(t.Value, t.Kind.Name)).ToList(),
            p.MatchByCnpjRoot,
            p.CanSchedulePayments);
}
