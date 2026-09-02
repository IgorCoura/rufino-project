namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.Bills;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PaymentOrderRepository : IPaymentOrderRepository
{
    // Derivado do Smart Enum, como no BillRepository: se a semântica de IsTerminal mudar,
    // a consulta acompanha.
    private static readonly PaymentOrderStatus[] ActiveStatuses =
        [.. Enumeration.GetAll<PaymentOrderStatus>().Where(s => !s.IsTerminal)];

    private readonly BillPaymentDbContext _context;

    public PaymentOrderRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(PaymentOrder order, CancellationToken cancellationToken = default)
        => await _context.PaymentOrders.AddAsync(order, cancellationToken);

    public Task<PaymentOrder?> GetAsync(TenantId tenantId, PaymentOrderId id, CancellationToken cancellationToken = default)
        => _context.PaymentOrders
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public Task<PaymentOrder?> GetActiveByBillAsync(TenantId tenantId, BillId billId, CancellationToken cancellationToken = default)
        => _context.PaymentOrders
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantId && o.BillId == billId && ActiveStatuses.Contains(o.Status),
                cancellationToken);

    /// <summary>
    /// A referência É o id da ordem (derivada, ver o agregado) — então resolver é ler o Guid.
    /// Referência ilegível devolve <c>null</c>: webhook com referência que não é nossa não é
    /// exceção, é "não conheço".
    /// </summary>
    public Task<PaymentOrder?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(externalReference, out var value))
            return Task.FromResult<PaymentOrder?>(null);

        var id = PaymentOrderId.From(value);
        return _context.PaymentOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
