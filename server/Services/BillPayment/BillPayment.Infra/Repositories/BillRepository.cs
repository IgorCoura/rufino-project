namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class BillRepository : IBillRepository
{
    // Derivado do Smart Enum em vez de listar Denied/Cancelled à mão: se a semântica de
    // OccupiesNaturalKey mudar, esta consulta acompanha. A propriedade em si não traduz
    // para SQL — Status é value-converted, então o filtro tem que ser sobre valores.
    private static readonly BillStatus[] StatusesThatOccupyTheKey =
        [.. Enumeration.GetAll<BillStatus>().Where(s => s.OccupiesNaturalKey)];

    private readonly BillPaymentDbContext _context;

    public BillRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(Bill bill, CancellationToken cancellationToken = default)
        => await _context.Bills.AddAsync(bill, cancellationToken);

    public Task<Bill?> GetAsync(TenantId tenantId, BillId id, CancellationToken cancellationToken = default)
        => _context.Bills
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, cancellationToken);

    /// <summary>
    /// <strong>Sem filtro de TenantId de propósito</strong> — travessia autorizada pelo ADR-008.
    /// Um compromisso é pago uma vez, e a colisão entre tenants de uma caixa compartilhada é
    /// exatamente o que precisa ser barrado. Devolve só <c>bool</c>: quem chama não consegue
    /// descobrir de quem é o boleto que colidiu.
    /// </summary>
    public Task<bool> ExistsActiveByDedupKeyAsync(string dedupKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dedupKey))
            return Task.FromResult(false);

        return _context.Bills
            .AsNoTracking()
            .AnyAsync(
                b => b.DedupKey == dedupKey && StatusesThatOccupyTheKey.Contains(b.Status),
                cancellationToken);
    }

    /// <summary>
    /// Mesma travessia, e o mesmo cuidado: a projeção traz o <c>TenantId</c> só para decidir
    /// <em>aqui dentro</em> se o id pode sair. Um boleto de outro tenant sai como
    /// <c>FoundInAnotherTenant</c>, sem id — o chamador não tem como reconstruir de quem é.
    /// </summary>
    /// <summary>
    /// Histórico do beneficiário para o aprendizado de expectativa. Traz os instrumentos porque
    /// é deles que sai o vencimento, e a carga é limitada pelo teto que o chamador informa.
    /// </summary>
    public async Task<IReadOnlyCollection<Bill>> ListByPayeeAsync(
        TenantId tenantId,
        PayeeId payeeId,
        int limit,
        CancellationToken cancellationToken = default)
        // Sem Include: Instruments é coluna jsonb com HasConversion, não navegação, e um Include
        // sobre ela estoura em runtime ("not a navigation property"). Ficou assim até 2026-08-28
        // e o único chamador — o aprendizado de expectativa disparado pelo outbox — nunca rodou:
        // a mensagem ia para dead-letter em silêncio. Checks é a única coleção owned, e é
        // auto-incluída.
        => await _context.Bills
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.PayeeId == payeeId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<DuplicateProbe> ProbeActiveDuplicateAsync(
        string dedupKey,
        TenantId tenantId,
        BillId excluding,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dedupKey))
            return DuplicateProbe.NotFound();

        var original = await _context.Bills
            .AsNoTracking()
            .Where(b => b.DedupKey == dedupKey
                && b.Id != excluding
                && StatusesThatOccupyTheKey.Contains(b.Status))
            .OrderBy(b => b.CreatedAt)
            .Select(b => new { b.Id, b.TenantId })
            .FirstOrDefaultAsync(cancellationToken);

        if (original is null)
            return DuplicateProbe.NotFound();

        return original.TenantId == tenantId
            ? DuplicateProbe.FoundInTenant(original.Id)
            : DuplicateProbe.FoundInAnotherTenant();
    }
}
