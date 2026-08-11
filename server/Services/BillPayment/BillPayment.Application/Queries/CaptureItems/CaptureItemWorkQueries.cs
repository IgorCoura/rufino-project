namespace BillPayment.Application.Queries.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureItemWorkQueries(BillPaymentDbContext context) : ICaptureItemWorkQueries
{
    public async Task<IReadOnlyList<PendingCaptureItem>> ListPendingAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Só o que ainda não foi processado. LinkFailed fica de fora de propósito: a nova
        // tentativa de um download que falhou é decisão de quem opera, não um laço automático
        // que insistiria para sempre contra um anexo que o provedor não entrega.
        return await context.CaptureItems
            .AsNoTracking()
            .Where(i => i.Status == CaptureItemStatus.Received)
            .OrderBy(i => i.ReceivedAt)
            .ThenBy(i => i.Id)
            .Take(limit)
            .Select(i => new PendingCaptureItem(i.TenantId.Value, i.Id.Value))
            .ToListAsync(cancellationToken);
    }
}
