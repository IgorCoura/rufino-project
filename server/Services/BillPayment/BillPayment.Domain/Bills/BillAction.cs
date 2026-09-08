namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que aconteceu com o boleto, no vocabulário de quem lê a trilha — não no da máquina de
/// estados.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é o mesmo conjunto que <see cref="BillStatus"/>, de propósito.</strong> Status diz
/// onde o boleto está; ação diz o que foi feito. Dois casos separam os dois: revalidar não muda
/// o status quando o veredito se repete, e continua sendo um evento que alguém precisa ver na
/// trilha; e <see cref="Unscheduled"/> e <see cref="Reverted"/> chegam ao mesmo destino por
/// motivos opostos, que a trilha tem obrigação de distinguir.
/// </para>
/// <para>
/// A entrada de histórico é gravada <strong>pelos próprios métodos ricos do agregado</strong>,
/// nunca por handler. É o que garante que trilha e máquina de estados não divirjam: quem muda o
/// estado registra, na mesma transação.
/// </para>
/// </remarks>
public sealed class BillAction : Enumeration
{
    /// <summary>O documento entrou — por e-mail, por portal ou por importação manual.</summary>
    public static readonly BillAction Captured = new(1, "Captured");

    /// <summary>As doze verificações rodaram. Vale para a primeira apuração e para cada revalidação.</summary>
    public static readonly BillAction Validated = new(2, "Validated");

    /// <summary>Uma pessoa autorizou o pagamento (ADR-007).</summary>
    public static readonly BillAction Approved = new(3, "Approved");

    /// <summary>Uma pessoa escolheu a data e mandou o boleto para a fila de pagamento.</summary>
    public static readonly BillAction Scheduled = new(4, "Scheduled");

    /// <summary>
    /// O agendamento foi desfeito e o boleto voltou a <c>Approved</c> — a aprovação continua de pé.
    /// </summary>
    public static readonly BillAction Unscheduled = new(5, "Unscheduled");

    /// <summary>Uma pessoa recusou o documento.</summary>
    public static readonly BillAction Denied = new(6, "Denied");

    /// <summary>Uma pessoa tirou o boleto do fluxo.</summary>
    public static readonly BillAction Cancelled = new(7, "Cancelled");

    /// <summary>
    /// Uma pessoa desfez uma recusa ou um cancelamento, e o boleto voltou à fila de decisão.
    /// </summary>
    public static readonly BillAction Reverted = new(8, "Reverted");

    /// <summary>O provedor aceitou a ordem. Reflexo da <c>PaymentOrder</c> (ADR-002).</summary>
    public static readonly BillAction HandedToProvider = new(9, "HandedToProvider");

    /// <summary>O dinheiro saiu.</summary>
    public static readonly BillAction Paid = new(10, "Paid");

    /// <summary>A execução do pagamento não fechou.</summary>
    public static readonly BillAction PaymentFailed = new(11, "PaymentFailed");

    /// <summary>Um boleto de pagamento falhado voltou à fila de decisão para nova tentativa.</summary>
    public static readonly BillAction Reopened = new(12, "Reopened");

    private BillAction(int id, string name) : base(id, name) { }

    /// <summary>
    /// Se a ação nasce de uma decisão de gente. As demais são reflexo de provedor ou de fila, e
    /// aparecem na trilha com autor "sistema".
    /// </summary>
    public bool IsHumanDecision
        => this == Approved || this == Scheduled || this == Unscheduled
        || this == Denied || this == Cancelled || this == Reverted || this == Reopened;
}
