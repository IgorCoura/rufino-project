namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class BillExpectationRepository : IBillExpectationRepository
{
    private readonly BillPaymentDbContext _context;

    public BillExpectationRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(BillExpectation expectation, CancellationToken cancellationToken = default)
        => await _context.BillExpectations.AddAsync(expectation, cancellationToken);

    // Os ciclos vêm juntos porque toda mutação da raiz passa por um deles — carregar a
    // expectativa sem eles obrigaria a uma segunda ida ao banco em todo caso de uso.
    public Task<BillExpectation?> GetAsync(
        TenantId tenantId,
        BillExpectationId id,
        CancellationToken cancellationToken = default)
        => _context.BillExpectations
            .Include(e => e.Cycles)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        PayeeId payeeId,
        string accountReference,
        BillExpectationId? excluding = null,
        CancellationToken cancellationToken = default)
        => _context.BillExpectations
            .AsNoTracking()
            .AnyAsync(
                e => e.TenantId == tenantId
                    && e.PayeeId == payeeId
                    && e.AccountReference == accountReference
                    && (excluding == null || e.Id != excluding.Value),
                cancellationToken);

    public async Task<IReadOnlyCollection<BillExpectation>> ListByPayeeAsync(
        TenantId tenantId,
        PayeeId payeeId,
        CancellationToken cancellationToken = default)
        => await _context.BillExpectations
            .Include(e => e.Cycles)
            .Where(e => e.TenantId == tenantId && e.PayeeId == payeeId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<BillExpectation>> ListByHintSourceAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        CancellationToken cancellationToken = default)
        => await _context.BillExpectations
            .Include(e => e.Cycles)
            .Where(e => e.TenantId == tenantId && e.IsActive && e.HintSourceId == sourceId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<BillExpectation>> ListByBlockedCaptureItemAsync(
        TenantId tenantId,
        CaptureItemId itemId,
        CancellationToken cancellationToken = default)
        => await _context.BillExpectations
            .Include(e => e.Cycles)
            .Where(e => e.TenantId == tenantId
                && e.Cycles.Any(c => c.BlockedByCaptureItemId == itemId))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// A fila do job. <strong>Não filtra por tenant e não é travessia</strong>: quem chama é o
    /// worker, que não age em nome de usuário nenhum e não projeta nada para tela — mesma
    /// natureza da varredura de caixas. Cada agregado carrega o próprio <c>TenantId</c>, e ele
    /// acompanha todo efeito que sair daqui.
    /// </summary>
    /// <remarks>
    /// O corte é por <c>LastSweptAt</c>, carimbado em toda passagem — inclusive na que não faz
    /// nada. Com ele o lote deixa de ser teto de cobertura: o worker pede lotes até a consulta
    /// voltar vazia, e a fila gira sozinha por mais expectativas ativas que a instalação tenha.
    /// Ordenar por <c>UpdatedAt</c>, como era antes, dava o oposto — o que nada faz mantém o
    /// carimbo antigo e monopoliza o lote.
    /// </remarks>
    public async Task<IReadOnlyCollection<BillExpectation>> ListActiveForSweepAsync(
        int batchSize,
        DateTime notSweptSince,
        CancellationToken cancellationToken = default)
        => await _context.BillExpectations
            .Include(e => e.Cycles)
            .Where(e => e.IsActive && e.LastSweptAt < notSweptSince)
            .OrderBy(e => e.LastSweptAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public void Remove(BillExpectation expectation) => _context.BillExpectations.Remove(expectation);
}
