namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que <c>Bill.RecordChecks</c> devolve a quem chamou.
/// </summary>
/// <remarks>
/// Existe para o handler não precisar inspecionar <c>bill.Checks</c> para montar a resposta —
/// ler a coleção do agregado para decidir qualquer coisa é a violação que a doutrina de
/// Handler proíbe. Se o handler precisa de um resultado da mutação, o método rico o devolve.
/// </remarks>
public sealed class ValidationOutcome : ValueObject
{
    public BillStatus Status { get; private set; } = default!;

    /// <summary>Quantas verificações reprovaram com peso bloqueante. Zero significa aprovável.</summary>
    public int BlockingFailures { get; private set; }

    /// <summary>Quantas verificações a tela precisa destacar — falha, aviso ou inconclusiva.</summary>
    public int AttentionItems { get; private set; }

    private ValidationOutcome() { }

    internal static ValidationOutcome Of(BillStatus status, int blockingFailures, int attentionItems)
        => new() { Status = status, BlockingFailures = blockingFailures, AttentionItems = attentionItems };

    public bool IsRejected => BlockingFailures > 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        yield return BlockingFailures;
        yield return AttentionItems;
    }
}
