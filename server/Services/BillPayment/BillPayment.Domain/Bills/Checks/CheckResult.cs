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
/// A severidade viaja aqui, e não só no <see cref="CheckType"/>, porque três checks escapam do
/// peso usual: banco cujas duas fontes autoritativas discordam, pagador extraído que contradiz
/// o cadastro, e origem explicitamente banida — todos <c>Advisory</c> por natureza que viram
/// <c>Blocking</c> naquela situação específica.
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

    /// <summary>Divergência que merece o olho do aprovador e não sustenta reprovação. Nunca bloqueia.</summary>
    public static CheckResult Warning(CheckType type, string reasonCode, string? evidence = null)
        => Create(type, CheckOutcome.Warning, type.DefaultSeverity, reasonCode, evidence);

    /// <summary>Não havia contra o que comparar.</summary>
    public static CheckResult Inconclusive(CheckType type, string reasonCode, string? evidence = null)
        => Create(type, CheckOutcome.Inconclusive, type.DefaultSeverity, reasonCode, evidence);

    /// <summary>O check não se aplica a este documento — ausência estrutural de dado.</summary>
    public static CheckResult Skipped(CheckType type, string reasonCode, string? evidence = null)
        => Create(type, CheckOutcome.Skipped, type.DefaultSeverity, reasonCode, evidence);

    /// <summary>Esta falha, sozinha, reprova o boleto?</summary>
    public bool IsBlockingFailure => Outcome.IsFailure && Severity == CheckSeverity.Blocking;

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
