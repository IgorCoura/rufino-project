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

    /// <summary>
    /// A submissão anterior teve desfecho desconhecido e <strong>não dá para provar</strong> se
    /// ela pagou. Só sai por gente, depois de conferir no provedor.
    /// </summary>
    /// <remarks>
    /// Nasceu do achado de sandbox de 2026-09-08: no trilho Pix o provedor <em>descarta</em> o
    /// <c>externalReference</c> que enviamos e <em>ignora</em> o filtro de busca por ele — ou
    /// seja, a consulta que autorizaria um reenvio seguro pode simplesmente não existir. Entre
    /// reenviar às cegas (pagar duas vezes) e parar pedindo ajuda, este BC para. É a única
    /// retenção que nasce de ignorância nossa, não de regra de negócio.
    /// </remarks>
    public static readonly PaymentOrderHold AwaitingManualReconciliation = new(4, "AwaitingManualReconciliation");

    private PaymentOrderHold(int id, string name) : base(id, name) { }
}
