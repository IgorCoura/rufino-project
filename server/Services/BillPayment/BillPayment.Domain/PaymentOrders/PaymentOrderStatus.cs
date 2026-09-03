namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Onde a ordem está na execução. A máquina inteira vive aqui — nenhum handler decide transição.
/// </summary>
/// <remarks>
/// <para>
/// <c>Draft</c> é nosso: a ordem existe e o provedor ainda não a conhece. Do <c>Pending</c> em
/// diante quem dita o estado é o provedor, e <see cref="PaymentOrder.ApplyProviderStatus"/> é
/// <strong>monotônica</strong>: webhook fora de ordem é ignorado, nunca lançado — reentrega e
/// atraso são o caso normal de webhook, não o excepcional.
/// </para>
/// <para>
/// <c>Failed</c> é terminal <strong>na ordem</strong>, de propósito (ADR-002): a história de
/// tentativas pertence ao lado da execução, e uma nova tentativa é uma ordem nova, nascida de
/// <c>Bill.ReopenForApproval</c> + nova aprovação — nunca a mesma ordem ressuscitada.
/// </para>
/// </remarks>
public sealed class PaymentOrderStatus : Enumeration
{
    /// <summary>Criada aqui, ainda não submetida ao provedor. É o estado da fila de submissão.</summary>
    public static readonly PaymentOrderStatus Draft = new(1, "Draft");

    /// <summary>O provedor aceitou e vai processar na data. Inclui a análise de risco dele.</summary>
    public static readonly PaymentOrderStatus Pending = new(2, "Pending");

    /// <summary>Em processamento bancário.</summary>
    public static readonly PaymentOrderStatus BankProcessing = new(3, "BankProcessing");

    /// <summary>Pago. Não regride — só <see cref="Refunded"/> vem depois.</summary>
    public static readonly PaymentOrderStatus Paid = new(4, "Paid");

    /// <summary>O provedor não conseguiu pagar, ou a submissão desistiu. Terminal na ordem.</summary>
    public static readonly PaymentOrderStatus Failed = new(5, "Failed", isTerminal: true);

    /// <summary>Cancelada antes da execução — por gente, pela recaptura, ou pelo provedor.</summary>
    public static readonly PaymentOrderStatus Cancelled = new(6, "Cancelled", isTerminal: true);

    /// <summary>O dinheiro voltou depois de pago. Terminal.</summary>
    public static readonly PaymentOrderStatus Refunded = new(7, "Refunded", isTerminal: true);

    /// <summary>Estado final: nenhuma mutação de execução é aceita a partir daqui.</summary>
    public bool IsTerminal { get; }

    private PaymentOrderStatus(int id, string name, bool isTerminal = false) : base(id, name)
        => IsTerminal = isTerminal;

    /// <summary>A ordem ainda espera desfecho do provedor — é o alvo da conciliação por polling.</summary>
    public bool AwaitsProviderOutcome => this == Pending || this == BankProcessing;

    public bool CanTransitionTo(PaymentOrderStatus target)
    {
        if (target is null)
            return false;

        return (this, target) switch
        {
            // Paid é quase-terminal: só o estorno do provedor vem depois.
            _ when this == Paid => target == Refunded,
            _ when IsTerminal => false,
            _ when this == Draft && (target == Pending || target == Failed || target == Cancelled) => true,
            // Pending/BankProcessing → Refunded SEM passar por Paid é tolerância deliberada a
            // reordenação de webhook: o REFUNDED chegando antes do PAID leva a ordem direto ao
            // desfecho que o provedor afirma — ao custo de PaidAt ficar nulo para sempre nessa
            // trilha (o Paid atrasado é ignorado pela monotônica). Refletir o provedor vence
            // reconstruir a história que ele não contou.
            _ when this == Pending && (target == BankProcessing || target == Paid || target == Failed || target == Cancelled || target == Refunded) => true,
            _ when this == BankProcessing && (target == Paid || target == Failed || target == Cancelled || target == Refunded) => true,
            _ => false,
        };
    }
}
