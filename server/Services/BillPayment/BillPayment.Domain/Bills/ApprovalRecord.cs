namespace BillPayment.Domain.Bills;

using BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Quem decidiu, quando, o quê, e por quê.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nenhum pagamento acontece sem um <c>UserId</c> aqui</strong> (ADR-007). Este VO é a
/// materialização dessa regra: o boleto não chega a <c>Approved</c> sem ele, e quem consultar a
/// trilha meses depois encontra o nome de uma pessoa, não "o sistema".
/// </para>
/// <para>
/// A observação é obrigatória em recusa e cancelamento, e opcional na aprovação. A assimetria é
/// deliberada: aprovar é o caminho esperado e explicar o óbvio vira ritual vazio; recusar é o
/// desvio, e é dele que alguém vai querer entender o motivo depois.
/// </para>
/// </remarks>
public sealed class ApprovalRecord : ValueObject
{
    public const int NOTE_MAX_LENGTH = 500;

    public UserId DecidedBy { get; private set; }
    public ApprovalDecision Decision { get; private set; } = default!;
    public DateTime DecidedAt { get; private set; }
    public string? Note { get; private set; }

    /// <summary>
    /// O nível de risco que o boleto exibia no instante da aprovação (ADR-015) — é a prova, na
    /// trilha de auditoria, de que o aprovador viu o alerta e decidiu mesmo assim. Nulo em
    /// recusa/cancelamento e em decisão anterior à classificação de risco.
    /// </summary>
    public RiskLevel? RiskAtDecision { get; private set; }

    private ApprovalRecord() { }

    public static ApprovalRecord Approve(
        UserId decidedBy, DateTime decidedAt, string? note, RiskLevel? riskAtDecision = null)
    {
        var record = Create(decidedBy, ApprovalDecision.Approved, decidedAt, note, noteRequired: false);
        record.RiskAtDecision = riskAtDecision;
        return record;
    }

    public static ApprovalRecord Deny(UserId decidedBy, DateTime decidedAt, string reason)
        => Create(decidedBy, ApprovalDecision.Denied, decidedAt, reason, noteRequired: true);

    public static ApprovalRecord Cancel(UserId decidedBy, DateTime decidedAt, string reason)
        => Create(decidedBy, ApprovalDecision.Cancelled, decidedAt, reason, noteRequired: true);

    private static ApprovalRecord Create(
        UserId decidedBy,
        ApprovalDecision decision,
        DateTime decidedAt,
        string? note,
        bool noteRequired)
    {
        if (decidedBy == UserId.Empty)
            throw BillErrors.ApproverRequired();

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is null && noteRequired)
            throw BillErrors.DecisionReasonRequired(decision.Name);

        if (trimmed is { Length: > NOTE_MAX_LENGTH })
            trimmed = trimmed[..NOTE_MAX_LENGTH];

        return new ApprovalRecord
        {
            DecidedBy = decidedBy,
            Decision = decision,
            DecidedAt = decidedAt,
            Note = trimmed,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DecidedBy;
        yield return Decision;
        yield return DecidedAt;
        yield return Note;
        yield return RiskAtDecision;
    }
}
