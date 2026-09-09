namespace BillPayment.Application.Queries.PayerProfiles;

using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
/// <remarks>
/// EF simples, e não ADO como a fila de submissão: aqui não há claim, não há <c>RETURNING</c> e
/// não há token <c>xmin</c> em jogo — só uma lista curta de ids.
/// </remarks>
internal sealed class PayerProfileWorkQueries(BillPaymentDbContext context) : IPayerProfileWorkQueries
{
    public async Task<IReadOnlyList<WebhookSweepTarget>> ListWithLinkedAccountAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var rows = await context.PayerProfiles
            .AsNoTracking()
            .Where(p => p.AsaasAccountRef != null)
            .OrderBy(p => p.UpdatedAt)
            .ThenBy(p => p.Id)
            .Take(limit)
            .Select(p => p.TenantId)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(id => new WebhookSweepTarget(id.Value))];
    }
}
