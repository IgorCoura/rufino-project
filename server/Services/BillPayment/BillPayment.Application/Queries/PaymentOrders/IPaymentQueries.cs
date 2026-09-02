namespace BillPayment.Application.Queries.PaymentOrders;

/// <summary>Leitura das ordens de pagamento — a fila operacional é esta lista filtrada por status.</summary>
public interface IPaymentQueries
{
    Task<PaymentOrderPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PaymentOrderDto?> GetAsync(
        Guid tenantId,
        Guid paymentOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>A ordem ativa (ou a mais recente) de um boleto — o detalhe do boleto a mostra.</summary>
    Task<PaymentOrderDto?> GetByBillAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);
}
