namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que o humano decidiu sobre o boleto.
/// </summary>
/// <remarks>
/// <strong>Recusar e cancelar não são a mesma coisa</strong>, apesar de os dois terminarem a
/// vida do boleto. <see cref="Denied"/> é um juízo sobre o documento — "não vou pagar isto";
/// <see cref="Cancelled"/> é sobre o processo — "não vale mais a pena tratar disto", e alcança
/// boleto que nem chegou a ser verificado. A distinção aparece no relatório de exceção, onde
/// recusa é sinal de qualidade da captura e cancelamento não é.
/// </remarks>
public sealed class ApprovalDecision : Enumeration
{
    public static readonly ApprovalDecision Approved = new(1, nameof(Approved));
    public static readonly ApprovalDecision Denied = new(2, nameof(Denied));
    public static readonly ApprovalDecision Cancelled = new(3, nameof(Cancelled));

    private ApprovalDecision(int id, string name) : base(id, name) { }
}
