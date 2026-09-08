namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O veredito de uma verificação, como o Domain Service o produz.
/// </summary>
/// <remarks>
/// <para>
/// Existe separado de <see cref="BillCheck"/> por um motivo prático: o serviço de validação não
/// lê relógio. Ele apura; <c>Bill.RecordChecks</c> é que carimba o instante. Assim o mesmo
/// conjunto de resultados é reprodutível em teste sem congelar tempo.
/// </para>
/// <para>
/// <para>
/// A severidade viaja aqui, e não só no <see cref="CheckType"/>, porque alguns desfechos escapam
/// do peso usual do check. Escapam <strong>para cima</strong>: banco cujas duas fontes
/// autoritativas discordam, pagador extraído que contradiz o cadastro, origem explicitamente
/// banida. E, desde 2026-09-08, escapam <strong>para baixo</strong>: os dois desfechos de nome do
/// check de beneficiário saem <see cref="CheckSeverity.Notice"/>, com teto de Atenção. Mesma
/// mecânica, direção oposta.
/// </para>
/// </para>
/// </remarks>
public sealed class CheckResult : ValueObject
{
    public const int EVIDENCE_MAX_LENGTH = 500;
    public const int REASON_CODE_MAX_LENGTH = 60;

    public CheckType Type { get; private set; } = default!;
    public CheckOutcome Outcome { get; private set; } = default!;
    public CheckSeverity Severity { get; private set; } = default!;

    /// <summary>Código estável que a UI traduz. Nulo só quando o check passou sem ressalva.</summary>
    public string? ReasonCode { get; private set; }

    /// <summary>Texto curto com os dois lados da comparação. Nunca carrega instrumento de pagamento.</summary>
    public string? Evidence { get; private set; }

    private CheckResult() { }

    /// <summary>Passou limpo, ou passou com uma ressalva registrada (o caso do cotejo só por nome).</summary>
    public static CheckResult Passed(CheckType type, string? reasonCode = null, string? evidence = null)
        => Create(type, CheckOutcome.Passed, type.DefaultSeverity, reasonCode, evidence);

    public static CheckResult Failed(CheckType type, string reasonCode, string? evidence = null, CheckSeverity? severity = null)
        => Create(type, CheckOutcome.Failed, severity ?? type.DefaultSeverity, reasonCode, evidence);

    /// <summary>
    /// Divergência que merece o olho do aprovador e não sustenta reprovação. Nunca reprova — mas
    /// desde 2026-09-08 <strong>pesa</strong> como Perigo, a menos que a severidade a rebaixe.
    /// </summary>
    public static CheckResult Warning(
        CheckType type, string reasonCode, string? evidence = null, CheckSeverity? severity = null)
        => Create(type, CheckOutcome.Warning, severity ?? type.DefaultSeverity, reasonCode, evidence);

    /// <summary>Não havia contra o que comparar.</summary>
    public static CheckResult Inconclusive(
        CheckType type, string reasonCode, string? evidence = null, CheckSeverity? severity = null)
        => Create(type, CheckOutcome.Inconclusive, severity ?? type.DefaultSeverity, reasonCode, evidence);

    /// <summary>O check não se aplica a este documento — ausência estrutural de dado.</summary>
    public static CheckResult Skipped(CheckType type, string reasonCode, string? evidence = null)
        => Create(type, CheckOutcome.Skipped, type.DefaultSeverity, reasonCode, evidence);

    /// <summary>
    /// Esta falha, sozinha, leva o boleto a Perigo (ou pior)? Critical conta como bloqueante —
    /// é um degrau ACIMA de Blocking, não ao lado.
    /// </summary>
    /// <summary>
    /// Esta falha, sozinha, impede a aprovação? Só <c>Blocking</c> e <c>Critical</c> — os dois
    /// degraus que ficam ACIMA de <c>Advisory</c>.
    /// </summary>
    /// <remarks>
    /// <strong>É afirmativo, e não "diferente de Advisory".</strong> Escrita pela negativa, a
    /// regra contava <see cref="CheckSeverity.Notice"/> junto no dia em que ele nasceu — e um
    /// boleto vencido, cujo teto é Atenção, passava a ser falha bloqueante. Pego pela suíte de
    /// integração em 2026-09-08.
    /// </remarks>
    public bool IsBlockingFailure
        => Outcome.IsFailure
        && (Severity == CheckSeverity.Blocking || Severity == CheckSeverity.Critical);

    /// <summary>Falha por declaração explícita do tenant — leva o boleto a Extremo Perigo.</summary>
    public bool IsCriticalFailure => Outcome.IsFailure && Severity == CheckSeverity.Critical;

    /// <summary>
    /// Quanto esta verificação pesa na classificação de risco. A régua vive em
    /// <c>RiskLevel.Of</c>, num lugar só — aqui é só o atalho de leitura.
    /// </summary>
    public RiskLevel RiskContribution => RiskLevel.Of(Outcome, Severity);

    private static CheckResult Create(
        CheckType type,
        CheckOutcome outcome,
        CheckSeverity severity,
        string? reasonCode,
        string? evidence)
    {
        if (type is null)
            throw BillErrors.CheckTypeRequired();

        // Só um Passed limpo pode não ter motivo: todo outro desfecho precisa dizer por quê,
        // porque é essa string que a tela de aprovação mostra e o relatório agrupa.
        var reason = Clamp(reasonCode, REASON_CODE_MAX_LENGTH);
        if (reason is null && outcome != CheckOutcome.Passed)
            throw BillErrors.CheckReasonRequired(type.Name);

        return new CheckResult
        {
            Type = type,
            Outcome = outcome,
            Severity = severity,
            ReasonCode = reason,
            Evidence = Clamp(evidence, EVIDENCE_MAX_LENGTH),
        };
    }

    private static string? Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Outcome;
        yield return Severity;
        yield return ReasonCode;
        yield return Evidence;
    }
}
