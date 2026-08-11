namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureItemRepository : ICaptureItemRepository
{
    private readonly BillPaymentDbContext _context;

    public CaptureItemRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(CaptureItem item, CancellationToken cancellationToken = default)
        => await _context.CaptureItems.AddAsync(item, cancellationToken);

    public Task<CaptureItem?> GetAsync(
        TenantId tenantId,
        CaptureItemId id,
        CancellationToken cancellationToken = default)
        => _context.CaptureItems
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string artifactKey,
        CancellationToken cancellationToken = default)
        => _context.CaptureItems
            .AsNoTracking()
            .AnyAsync(
                i => i.TenantId == tenantId
                    && i.SourceId == sourceId
                    && i.ExternalMessageId == externalMessageId
                    && i.ArtifactKey == artifactKey,
                cancellationToken);

    public async Task<CaptureItemId?> FindOriginalByContentHashAsync(
        TenantId tenantId,
        string contentHash,
        CaptureItemId excludingId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            return null;

        // O original é o mais antigo, e um item já descartado não serve de original — apontar
        // para ele encadearia duplicatas e a trilha deixaria de levar ao artefato de verdade.
        var original = await _context.CaptureItems
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && i.ContentHash == contentHash
                && i.Id != excludingId
                && i.Status != CaptureItemStatus.Discarded)
            .OrderBy(i => i.CreatedAt)
            .Select(i => (CaptureItemId?)i.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return original;
    }

    public void Remove(CaptureItem item) => _context.CaptureItems.Remove(item);
}
