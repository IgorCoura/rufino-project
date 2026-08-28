namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CapturedMessageRepository : ICapturedMessageRepository
{
    private readonly BillPaymentDbContext _context;

    public CapturedMessageRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(CapturedMessage message, CancellationToken cancellationToken = default)
        => await _context.CapturedMessages.AddAsync(message, cancellationToken);

    public Task<CapturedMessage?> GetAsync(
        TenantId tenantId,
        CapturedMessageId id,
        CancellationToken cancellationToken = default)
        => _context.CapturedMessages
            .Include(m => m.Artifacts)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == id, cancellationToken);

    public Task<CapturedMessage?> FindByExternalMessageIdAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        CancellationToken cancellationToken = default)
        => _context.CapturedMessages
            .Include(m => m.Artifacts)
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId
                    && m.SourceId == sourceId
                    && m.ExternalMessageId == externalMessageId,
                cancellationToken);

    /// <summary>
    /// Vencidos pela janela, <strong>exceto os que produziram boleto</strong>.
    /// </summary>
    /// <remarks>
    /// O filtro do boleto vive aqui e não na política: é sobre quais registros existem, não sobre
    /// por quanto tempo guardar. Registro de e-mail que virou pagamento é trilha de auditoria e
    /// não expira com prazo nenhum — a purga alcança descarte, não dinheiro.
    /// </remarks>
    public async Task<IReadOnlyList<CapturedMessage>> ListPurgeableAsync(
        TenantId tenantId,
        DateTime olderThan,
        int limit,
        CancellationToken cancellationToken = default)
        => await _context.CapturedMessages
            .Include(m => m.Artifacts)
            .Where(m => m.TenantId == tenantId
                && m.ReceivedAt < olderThan
                && !m.Artifacts.Any(a => a.BillId != null))
            .OrderBy(m => m.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Remove(CapturedMessage message) => _context.CapturedMessages.Remove(message);
}
