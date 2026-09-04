namespace BillPayment.Domain.PaymentOrders;

/// <summary>
/// A tradução do vocabulário de status do provedor para o nosso — <strong>um mapa só</strong>,
/// consultado pelo adapter (conciliação, adoção) e pelo webhook. Antes eram dois switches
/// idênticos em camadas diferentes; um status novo do provedor entrando num e não no outro
/// faria webhook e conciliação discordarem em silêncio.
/// </summary>
/// <remarks>
/// <para>
/// Função pura sobre o vocabulário PUBLICADO do provedor — sem I/O, sem DTO, no molde do
/// <c>IWorkingDayCalendar</c> calculado. O nome cru viaja no <c>RawStatus</c> do retrato; aqui
/// só mora a tradução.
/// </para>
/// <para>
/// <strong>Status desconhecido cai em <c>Pending</c> de propósito</strong>: mantém a conciliação
/// vigiando em vez de declarar um desfecho que o provedor não afirmou. O evento de webhook usa o
/// mesmo mapa do pague-contas — <c>BILL_PAID</c> sem o prefixo é <c>PAID</c>.
/// </para>
/// </remarks>
public static class ProviderStatusCatalog
{
    public static PaymentOrderStatus FromBillPayment(string? raw)
        => raw?.ToUpperInvariant() switch
        {
            "PENDING" or "AWAITING_CHECKOUT_RISK_ANALYSIS_REQUEST" or "SCHEDULED" => PaymentOrderStatus.Pending,
            "BANK_PROCESSING" => PaymentOrderStatus.BankProcessing,
            "PAID" => PaymentOrderStatus.Paid,
            "FAILED" => PaymentOrderStatus.Failed,
            "CANCELLED" => PaymentOrderStatus.Cancelled,
            "REFUNDED" => PaymentOrderStatus.Refunded,
            _ => PaymentOrderStatus.Pending,
        };

    public static PaymentOrderStatus FromPixPayment(string? raw)
        => raw?.ToUpperInvariant() switch
        {
            "AWAITING_BALANCE_VALIDATION" or "SCHEDULED" or "AWAITING_INSTANT_PAYMENT_ACCOUNT_BALANCE"
                or "AWAITING_CRITICAL_ACTION_AUTHORIZATION" or "AWAITING_CHECKOUT_RISK_ANALYSIS_REQUEST"
                    => PaymentOrderStatus.Pending,
            "REQUESTED" or "BANK_PROCESSING" => PaymentOrderStatus.BankProcessing,
            "DONE" => PaymentOrderStatus.Paid,
            "REFUSED" or "FAILED" or "ERROR" => PaymentOrderStatus.Failed,
            "CANCELLED" => PaymentOrderStatus.Cancelled,
            "REFUNDED" => PaymentOrderStatus.Refunded,
            _ => PaymentOrderStatus.Pending,
        };
}
