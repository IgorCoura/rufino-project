namespace BillPayment.Application.Queries.Payees;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PayeeQueries(BillPaymentDbContext context) : IPayeeQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<PayeePage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.Payees.AsNoTracking().Where(p => p.TenantId == tenant);

        // Keyset ascendente por (CreatedAt, Id) — o Id desempata, senão um lote gravado no mesmo
        // instante torna inalcançável tudo além da primeira página (ver CursorCodec).
        if (CursorCodec.TryDecode(cursor, out var afterCreatedAt, out var afterId))
        {
            var afterPayeeId = PayeeId.From(afterId);

            query = query.Where(p =>
                p.CreatedAt > afterCreatedAt || (p.CreatedAt == afterCreatedAt && p.Id > afterPayeeId));
        }

        var rows = await query
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new PayeePage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<PayeeDto?> GetAsync(
        Guid tenantId,
        Guid payeeId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = PayeeId.From(payeeId);

        var payee = await context.Payees
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant && p.Id == id, cancellationToken);

        return payee is null ? null : ToDto(payee);
    }

    public async Task<PayeeDto?> FindByTaxIdAsync(
        Guid tenantId,
        string taxId,
        CancellationToken cancellationToken = default)
    {
        // Passa pelo VO antes de comparar: o conversor grava só dígitos, e buscar pelo texto
        // formatado não encontraria nada. Documento malformado é ausência, não erro.
        TaxId parsed;
        try
        {
            parsed = TaxId.Parse(taxId);
        }
        catch (DomainException)
        {
            return null;
        }

        var tenant = TenantId.From(tenantId);

        var payee = await context.Payees
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant && p.TaxId == parsed, cancellationToken);

        return payee is null ? null : ToDto(payee);
    }

    private static PayeeDto ToDto(Payee p)
        => new(
            p.Id.Value,
            p.LegalName,
            p.TaxId.Value,
            p.TaxId.Kind.Name,
            p.Aliases.ToList(),
            p.AcceptedBanks.Select(b => b.Value).ToList(),
            new PayeeAmountPolicyDto(
                p.AmountPolicy.Kind.Name,
                p.AmountPolicy.ExpectedAmount?.Amount,
                p.AmountPolicy.TolerancePercent,
                p.AmountPolicy.MinAmount?.Amount,
                p.AmountPolicy.MaxAmount?.Amount,
                p.AmountPolicy.IsConclusive),
            p.IsActive);
}
