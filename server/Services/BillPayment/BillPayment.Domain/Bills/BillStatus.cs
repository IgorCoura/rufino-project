namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Onde o boleto está na sua história. A máquina de estados inteira vive aqui — nenhum
/// handler decide transição.
/// </summary>
/// <remarks>
/// <c>Scheduled</c> em diante o <c>Bill</c> é <strong>espelho</strong> da <c>PaymentOrder</c>:
/// a verdade da execução é dela, e o Bill só reflete por evento (<c>ADR-002</c>).
/// </remarks>
public sealed class BillStatus : Enumeration
{
    public static readonly BillStatus Captured = new(1, "Captured");
    public static readonly BillStatus AwaitingApproval = new(2, "AwaitingApproval");
    public static readonly BillStatus Rejected = new(3, "Rejected");
    public static readonly BillStatus Approved = new(4, "Approved");
    public static readonly BillStatus Denied = new(5, "Denied", isTerminal: true);
    public static readonly BillStatus Scheduled = new(6, "Scheduled");
    public static readonly BillStatus Paid = new(7, "Paid", isTerminal: true);
    public static readonly BillStatus Failed = new(8, "Failed");
    public static readonly BillStatus Cancelled = new(9, "Cancelled", isTerminal: true);

    /// <summary>
    /// Estado final do <strong>fluxo</strong>: a máquina não sai daqui por transição normal
    /// (BLP.BIL07).
    /// </summary>
    /// <remarks>
    /// <strong>Há uma única exceção, e ela é nomeada:</strong> <see cref="CanBeUndone"/>.
    /// Recusar e cancelar são decisões de gente, e gente erra — <c>Bill.UndoDecision</c> desfaz
    /// as duas por um caminho próprio, com alçada própria, sem passar pela matriz de transições.
    /// Nenhum outro método escapa daqui, e a reversão não vale para <c>Paid</c>: dinheiro que
    /// saiu não volta por decisão nossa.
    /// </remarks>
    public bool IsTerminal { get; }

    /// <summary>
    /// Se uma decisão humana terminal pode ser desfeita, devolvendo o boleto à fila de decisão.
    /// </summary>
    /// <remarks>
    /// Só <see cref="Denied"/> e <see cref="Cancelled"/> — os dois estados que já liberam a chave
    /// natural (<see cref="OccupiesNaturalKey"/>), o que torna a volta coerente: a chave estava
    /// livre e o boleto volta a ocupá-la. <see cref="Paid"/> é terminal de verdade.
    /// </remarks>
    public bool CanBeUndone => this == Denied || this == Cancelled;

    /// <summary>
    /// Se o boleto ainda ocupa a chave natural do instrumento na deduplicação global.
    /// <c>Denied</c> e <c>Cancelled</c> liberam a chave — o compromisso não vai ser pago por
    /// eles e o documento pode legitimamente ser reimportado. <c>Paid</c> <strong>não</strong>
    /// libera: é justamente a duplicata de algo já pago que precisa ser barrada.
    /// </summary>
    public bool OccupiesNaturalKey => this != Denied && this != Cancelled;

    private BillStatus(int id, string name, bool isTerminal = false) : base(id, name)
        => IsTerminal = isTerminal;

    public bool CanTransitionTo(BillStatus target)
    {
        if (target is null || IsTerminal)
            return false;

        return (this, target) switch
        {
            _ when this == Captured && (target == AwaitingApproval || target == Rejected || target == Cancelled) => true,
            _ when this == Rejected && (target == AwaitingApproval || target == Cancelled) => true,
            _ when this == AwaitingApproval && (target == Approved || target == Denied || target == Rejected || target == Cancelled) => true,
            // Approved → Failed: a SUBMISSÃO foi recusada pelo provedor antes de agendar. Sem
            // esta aresta o boleto ficaria "aprovado, agendamento em processamento" para sempre
            // — a falha visível na ordem e invisível no espelho (fase 3, 2026-09-02).
            _ when this == Approved && (target == Scheduled || target == Failed || target == AwaitingApproval || target == Rejected || target == Cancelled) => true,
            // Scheduled → Approved: o AGENDAMENTO foi desfeito, não o boleto. Até 2026-09-08 a
            // ordem cancelada levava o boleto a Cancelled (terminal) — cancelar um agendamento
            // para trocar a data matava o boleto, e a aprovação vigente ia junto. A aprovação é
            // de gente e continua de pé; quem some é a data.
            _ when this == Scheduled && (target == Paid || target == Failed || target == Approved || target == Cancelled) => true,
            _ when this == Failed && (target == AwaitingApproval || target == Cancelled) => true,
            _ => false,
        };
    }

    /// <summary>
    /// A validação ainda pode rodar sobre um boleto neste estado?
    /// </summary>
    /// <remarks>
    /// A partir de <see cref="Scheduled"/> a verdade da execução é da <c>PaymentOrder</c>
    /// (ADR-002) e o dinheiro já está em movimento: revalidar ali mudaria o veredito de algo
    /// que não dá mais para desfazer por este caminho.
    /// </remarks>
    public bool AcceptsValidation
        => this == Captured || this == Rejected || this == AwaitingApproval || this == Approved;

    /// <summary>
    /// Uma pessoa já autorizou o pagamento deste boleto — aprovado, agendado, tentado ou pago.
    /// </summary>
    /// <remarks>
    /// É o que trava a recaptura do e-mail de origem: refazer a triagem apagaria e recriaria o
    /// boleto, e um boleto com dinheiro comprometido não se refaz por trás de quem decidiu.
    /// <c>Failed</c> entra porque o pagamento foi tentado — o que fazer com ele (retentar, avisar
    /// para pagar à mão) é decisão da fase de pagamento, não da captura. <c>Denied</c> e
    /// <c>Cancelled</c> ficam de fora: liberaram a chave e o humano pode decidir de novo.
    /// </remarks>
    public bool IsCommittedToPayment
        => this == Approved || this == Scheduled || this == Failed || this == Paid;
}
