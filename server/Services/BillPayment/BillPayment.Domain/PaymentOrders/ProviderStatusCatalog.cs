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
            // AWAITING_CRITICAL_ACTION_AUTHORIZATION entrou aqui em 2026-09-08: estava mapeado
            // só no trilho Pix e caía no default do lado do boleto. O destino é o mesmo
            // (Pending), mas explicitá-lo é o que impede a próxima leitura de supor que o
            // provedor nunca usa esse status no pague-contas.
            "PENDING" or "AWAITING_CHECKOUT_RISK_ANALYSIS_REQUEST" or "SCHEDULED"
                or "AWAITING_CRITICAL_ACTION_AUTHORIZATION" or "AWAITING_BALANCE_VALIDATION"
                    => PaymentOrderStatus.Pending,
            "BANK_PROCESSING" => PaymentOrderStatus.BankProcessing,
            "PAID" => PaymentOrderStatus.Paid,
            "FAILED" => PaymentOrderStatus.Failed,
            "CANCELLED" => PaymentOrderStatus.Cancelled,
            "REFUNDED" => PaymentOrderStatus.Refunded,
            _ => PaymentOrderStatus.Pending,
        };

    /// <summary>
    /// A tradução do vocabulário de <c>transfer</c> — o objeto que os webhooks
    /// <c>TRANSFER_*</c> carregam, e que é o espelho de toda saída de Pix.
    /// </summary>
    /// <remarks>
    /// MEDIDO EM SANDBOX (2026-09-08): <strong>transfer e transação Pix falam vocabulários
    /// diferentes para o mesmo fato</strong> — a transação diz
    /// <c>AWAITING_CRITICAL_ACTION_AUTHORIZATION</c> enquanto o transfer diz <c>PENDING</c>;
    /// <c>REFUSED</c> de um lado é <c>FAILED</c> do outro. Traduzir o transfer com o mapa da
    /// transação erraria, e por isso este mapa existe separado em vez de reusar
    /// <see cref="FromPixPayment"/>.
    /// </remarks>
    public static PaymentOrderStatus FromTransfer(string? raw)
        => raw?.ToUpperInvariant() switch
        {
            "PENDING" or "SCHEDULED" or "AWAITING_CRITICAL_ACTION_AUTHORIZATION" => PaymentOrderStatus.Pending,
            "BANK_PROCESSING" or "IN_BANK_ACCOUNT" => PaymentOrderStatus.BankProcessing,
            "DONE" => PaymentOrderStatus.Paid,
            "FAILED" or "BLOCKED" or "REFUSED" => PaymentOrderStatus.Failed,
            "CANCELLED" => PaymentOrderStatus.Cancelled,
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
