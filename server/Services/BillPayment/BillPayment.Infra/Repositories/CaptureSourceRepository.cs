namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureSourceRepository : ICaptureSourceRepository
{
    private readonly BillPaymentDbContext _context;

    public CaptureSourceRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(CaptureSource source, CancellationToken cancellationToken = default)
        => await _context.CaptureSources.AddAsync(source, cancellationToken);

    public Task<CaptureSource?> GetAsync(
        TenantId tenantId,
        CaptureSourceId id,
        CancellationToken cancellationToken = default)
        => _context.CaptureSources
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        string normalizedAddress,
        CancellationToken cancellationToken = default)
        => _context.CaptureSources
            .AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.Address == normalizedAddress, cancellationToken);

    public async Task<IReadOnlyList<CaptureSource>> ListEnabledAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
        => await _context.CaptureSources
            .Where(s => s.TenantId == tenantId && s.IsEnabled)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CaptureSource>> ListEnabledForWorkerAsync(
        int limit,
        CancellationToken cancellationToken = default)
        => await _context.CaptureSources
            .Where(s => s.IsEnabled)
            // Quem esperou mais vai primeiro, e fonte nunca sincronizada (LastSyncAt nulo) vem
            // antes de todas — senão uma caixa recém-conectada ficaria atrás da fila para sempre.
            .OrderBy(s => s.LastSyncAt ?? DateTime.MinValue)
            .Take(limit)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// <strong>Travessia de tenant autorizada nº 1 (ADR-008).</strong> Único caminho de código
    /// que consulta <c>capture_sources</c> sem filtrar por tenant.
    /// </summary>
    /// <remarks>
    /// Devolve <c>bool</c> e nada mais — o <c>AnyAsync</c> não é otimização, é o contrato: não
    /// existe projeção aqui de onde alguém possa extrair id, nome ou contagem do outro tenant.
    /// Acrescentar um <c>Select</c> ou trocar por <c>CountAsync</c> quebra o ADR-008.
    /// </remarks>
    public Task<bool> IsAddressMonitoredByAnyTenantAsync(
        string normalizedAddress,
        TenantId excludingTenantId,
        CancellationToken cancellationToken = default)
        => _context.CaptureSources
            .AsNoTracking()
            .AnyAsync(
                s => s.Address == normalizedAddress && s.TenantId != excludingTenantId,
                cancellationToken);

    public void Remove(CaptureSource source) => _context.CaptureSources.Remove(source);
}
