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

    /// <summary>
    /// A classificação que a validação produziu.
    /// </summary>
    /// <remarks>
    /// Entrou em 2026-09-08 (ADR-020) porque as duas contagens deixaram de descrever o desfecho:
    /// com falha advisory, aviso e inconclusivo valendo Perigo, "zero bloqueios" passou a
    /// conviver com <c>Danger</c>. Quem quer saber como o boleto ficou lê isto, não as contagens.
    /// </remarks>
    public RiskLevel Risk { get; private set; } = default!;

    /// <summary>Quantas verificações reprovaram com peso bloqueante.</summary>
    public int BlockingFailures { get; private set; }

    /// <summary>Quantas verificações a tela precisa destacar — falha, aviso ou inconclusiva.</summary>
    public int AttentionItems { get; private set; }

    /// <summary>
    /// A aprovação que existia continuou de pé: nada que o aprovador consentiu mudou nesta
    /// rodada.
    /// </summary>
    /// <remarks>
    /// Entrou em 2026-09-10. Quem revalida precisa saber qual das duas coisas aconteceu — "siga
    /// para o agendamento" ou "isto voltou para a fila de decisão" —, e deduzir isso do
    /// <see cref="Status"/> exigiria saber em que estado o boleto estava antes da chamada, que é
    /// justamente o que quem chamou já não tem.
    /// </remarks>
    public bool ApprovalPreserved { get; private set; }

    private ValidationOutcome() { }

    internal static ValidationOutcome Of(
        BillStatus status,
        RiskLevel risk,
        int blockingFailures,
        int attentionItems,
        bool approvalPreserved = false)
        => new()
        {
            Status = status,
            Risk = risk,
            BlockingFailures = blockingFailures,
            AttentionItems = attentionItems,
            ApprovalPreserved = approvalPreserved,
        };

    public bool IsRejected => BlockingFailures > 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        yield return Risk;
        yield return BlockingFailures;
        yield return AttentionItems;
        yield return ApprovalPreserved;
    }
}
