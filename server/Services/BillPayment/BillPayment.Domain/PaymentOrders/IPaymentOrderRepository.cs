namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.Bills;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate.
/// </summary>
/// <remarks>
/// <see cref="GetByExternalReferenceAsync"/> não recebe <c>TenantId</c> e <strong>não é
/// travessia</strong>: quem chama é o webhook do provedor — um processo sem <c>HttpContext</c>,
/// como as varreduras de worker — e a referência resolve o tenant, não o contrário. Nada dele
/// responde a um usuário. Toda consulta a serviço de gente filtra por <c>TenantId</c>.
/// </remarks>
public interface IPaymentOrderRepository
{
    Task AddAsync(PaymentOrder order, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<PaymentOrder?> GetAsync(TenantId tenantId, PaymentOrderId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A ordem ativa (não terminal) de um boleto, se houver. É o que torna o handler de
    /// aprovação idempotente: o outbox entrega ao menos uma vez, e a segunda entrega precisa
    /// encontrar a ordem da primeira em vez de criar outra.
    /// </summary>
    Task<PaymentOrder?> GetActiveByBillAsync(TenantId tenantId, BillId billId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a ordem pela referência que o provedor devolve. Consumidor: webhook e
    /// conciliação — processos de instalação, nunca resposta a usuário (ver remarks da interface).
    /// </summary>
    Task<PaymentOrder?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);
}
