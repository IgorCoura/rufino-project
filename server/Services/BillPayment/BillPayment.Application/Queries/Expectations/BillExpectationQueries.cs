namespace BillPayment.Application.Queries.Expectations;

using BillPayment.Domain.Expectations;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
internal sealed class BillExpectationQueries(BillPaymentDbContext context, TimeProvider clock)
    : IBillExpectationQueries
{
    public async Task<BillExpectationPage> ListAsync(
        Guid tenantId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var size = limit is < 1 or > 100 ? 20 : limit;

        var query = context.BillExpectations
            .AsNoTracking()
            .Include(e => e.Cycles)
            .Where(e => e.TenantId == tenant);

        // Keyset com (CreatedAt, Id): CreatedAt empata quando o aprendizado cria várias
        // expectativas no mesmo ciclo do job, e o desempate pelo Id é o que impede a página 2 de
        // voltar vazia. A direção do desempate acompanha a da chave.
        if (CursorCodec.TryDecode(cursor, out var createdAt, out var lastId))
        {
            var lastExpectationId = BillExpectationId.From(lastId);

            query = query.Where(e =>
                e.CreatedAt > createdAt || (e.CreatedAt == createdAt && e.Id > lastExpectationId));
        }

        var rows = await query
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Take(size + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > size;
        var page = hasMore ? rows.Take(size).ToList() : rows;

        var next = hasMore && page.Count > 0
            ? CursorCodec.Encode(page[^1].CreatedAt, page[^1].Id.Value)
            : null;

        return new BillExpectationPage(page.ConvertAll(ToDto), next);
    }

    public async Task<BillExpectationDto?> GetAsync(
        Guid tenantId, Guid expectationId, CancellationToken cancellationToken = default)
    {
        var expectation = await context.BillExpectations
            .AsNoTracking()
            .Include(e => e.Cycles)
            .FirstOrDefaultAsync(
                e => e.TenantId == TenantId.From(tenantId)
                    && e.Id == BillExpectationId.From(expectationId),
                cancellationToken);

        return expectation is null ? null : ToDto(expectation);
    }

    public async Task<PendingExpectationsView> ListPendingAsync(
        Guid tenantId, int dueSoonWindowDays, CancellationToken cancellationToken = default)
    {
        var window = dueSoonWindowDays is < 1 or > 90 ? 7 : dueSoonWindowDays;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var expectations = await context.BillExpectations
            .AsNoTracking()
            .Include(e => e.Cycles)
            .Where(e => e.TenantId == TenantId.From(tenantId) && e.IsActive)
            .ToListAsync(cancellationToken);

        var missing = new List<PendingExpectationDto>();
        var captureFailed = new List<PendingExpectationDto>();
        var dueSoon = new List<PendingExpectationDto>();

        foreach (var expectation in expectations)
        {
            foreach (var cycle in expectation.Cycles.Where(c => c.Status.IsOpen))
            {
                var dto = ToPendingDto(expectation, cycle);

                if (cycle.Status == CycleStatus.Missing)
                    missing.Add(dto);
                else if (cycle.Status == CycleStatus.PartiallyCaptured)
                    captureFailed.Add(dto);
                else if (cycle.ExpectedDueDate <= today.AddDays(window))
                    dueSoon.Add(dto);
            }
        }

        // O mais antigo primeiro em todas: é o que está há mais tempo sem solução.
        missing.Sort(ByDueDate);
        captureFailed.Sort(ByDueDate);
        dueSoon.Sort(ByDueDate);

        return new PendingExpectationsView(missing, captureFailed, dueSoon);
    }

    private static int ByDueDate(PendingExpectationDto a, PendingExpectationDto b)
        => a.ExpectedDueDate.CompareTo(b.ExpectedDueDate);

    private static BillExpectationDto ToDto(BillExpectation e)
        => new(
            e.Id.Value,
            e.PayeeId.Value,
            e.AccountReference,
            e.Label,
            e.Recurrence.Name,
            e.ExpectedDueDay,
            e.ObservedLeadDays,
            e.AlertLeadDays,
            e.Origin.Name,
            e.ObservationCount,
            e.IsActive,
            e.PausedUntil,
            e.Cycles
                .OrderByDescending(c => c.ExpectedDueDate)
                .Select(ToCycleDto)
                .ToList());

    private static ExpectationCycleDto ToCycleDto(ExpectationCycle c)
        => new(
            c.Id.Value,
            c.Competence.ToString(),
            c.ExpectedDueDate,
            c.AlertAt,
            c.Status.Name,
            c.MissReason?.Name,
            c.MissReason?.Arrived,
            c.FulfilledByBillId?.Value,
            c.BlockedByCaptureItemId?.Value,
            LastAlert(c));

    private static PendingExpectationDto ToPendingDto(BillExpectation e, ExpectationCycle c)
        => new(
            e.Id.Value,
            c.Id.Value,
            e.Label,
            c.Competence.ToString(),
            c.ExpectedDueDate,
            c.Status.Name,
            c.MissReason?.Name,
            c.MissReason?.Arrived,
            c.BlockedByCaptureItemId?.Value,
            LastAlert(c));

    private static string? LastAlert(ExpectationCycle c)
        => c.Alerts.Count == 0 ? null : c.Alerts.OrderBy(a => a.SentAt).Last().Level.Name;
}
