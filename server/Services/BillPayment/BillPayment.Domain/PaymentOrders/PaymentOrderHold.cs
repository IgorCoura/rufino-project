namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Por que uma ordem em <c>Draft</c> está fora da fila de submissão. Estado visível, nunca
/// silêncio — o modo de falha deste BC é a conta que não anda sem ninguém saber (ADR-014).
/// </summary>
/// <remarks>
/// A reivindicação da fila só pega ordem com <see cref="None"/>: quem está retida não gasta
/// tentativa nem aparece como falha. <see cref="AwaitingAccount"/> destrava sozinha quando o
/// tenant vincula a chave (a varredura reconfere); <see cref="AwaitingConfirmation"/> só
/// destrava por gente, com autor gravado — é a regra do vencido do <c>ADR-017</c>.
/// </remarks>
public sealed class PaymentOrderHold : Enumeration
{
    /// <summary>Sem retenção: a ordem é elegível para a fila de submissão.</summary>
    public static readonly PaymentOrderHold None = new(1, "None");

    /// <summary>O tenant não tem conta de pagamento vinculada. Vincular a chave destrava.</summary>
    public static readonly PaymentOrderHold AwaitingAccount = new(2, "AwaitingAccount");

    /// <summary>
    /// A execução seria imediata (boleto vencido) e ninguém confirmou — ADR-017. Só sai por
    /// <see cref="PaymentOrder.ConfirmImmediateExecution"/>, que grava quem confirmou.
    /// </summary>
    public static readonly PaymentOrderHold AwaitingConfirmation = new(3, "AwaitingConfirmation");

    private PaymentOrderHold(int id, string name) : base(id, name) { }
}
