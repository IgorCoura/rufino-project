namespace BillPayment.Infra.Repositories;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PayeeRepository : IPayeeRepository
{
    private readonly BillPaymentDbContext _context;

    public PayeeRepository(BillPaymentDbContext context) => _context = context;

    public async Task AddAsync(Payee payee, CancellationToken cancellationToken = default)
        => await _context.Payees.AddAsync(payee, cancellationToken);

    public Task<Payee?> GetAsync(TenantId tenantId, PayeeId id, CancellationToken cancellationToken = default)
        => _context.Payees
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken);

    // A comparação é pelo Value Object inteiro: o conversor grava só os dígitos, então
    // "11.222.333/0001-81" e "11222333000181" chegam ao SQL como o mesmo valor.
    public Task<bool> ExistsAsync(TenantId tenantId, TaxId taxId, CancellationToken cancellationToken = default)
        => _context.Payees
            .AsNoTracking()
            .AnyAsync(p => p.TenantId == tenantId && p.TaxId == taxId, cancellationToken);

    // Tracked de propósito: a validação pode acabar em "aprender o apelido" sobre o mesmo
    // beneficiário, e a carga sem rastreamento obrigaria a recarregar para mutar.
    public async Task<IReadOnlyCollection<Payee>> ListByTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
        => await _context.Payees
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Remove(Payee payee) => _context.Payees.Remove(payee);
}
