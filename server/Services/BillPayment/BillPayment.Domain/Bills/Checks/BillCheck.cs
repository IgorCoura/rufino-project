namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Uma verificação apurada e gravada no boleto, com o instante em que foi apurada.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Value Object, apesar de o doc 02 chamá-lo de entidade interna.</strong> Um check
/// não tem ciclo de vida próprio: <c>RecordChecks</c> substitui o conjunto inteiro a cada
/// validação (ADR-003), não edita um item. Identidade seria ficção — o que identifica um check
/// dentro do boleto é o <see cref="Type"/>, e disso a chave da tabela filha já dá conta.
/// Persistido em <c>bill_checks</c> como coleção owned, como o ADR decidiu.
/// </para>
/// <para>
/// <strong>Nunca é editável por endpoint.</strong> O aprovador aprova <em>apesar</em> do check,
/// e essa decisão fica gravada com motivo; o check permanece <c>Failed</c> para sempre.
/// </para>
/// </remarks>
public sealed class BillCheck : ValueObject
{
    public CheckType Type { get; private set; } = default!;
    public CheckOutcome Outcome { get; private set; } = default!;
    public CheckSeverity Severity { get; private set; } = default!;
    public string? ReasonCode { get; private set; }
    public string? Evidence { get; private set; }
    public DateTime EvaluatedAt { get; private set; }

    private BillCheck() { }

    internal static BillCheck From(CheckResult result, DateTime evaluatedAt)
        => new()
        {
            Type = result.Type,
            Outcome = result.Outcome,
            Severity = result.Severity,
            ReasonCode = result.ReasonCode,
            Evidence = result.Evidence,
            EvaluatedAt = evaluatedAt,
        };

    public bool IsBlockingFailure => Outcome.IsFailure && Severity != CheckSeverity.Advisory;

    /// <summary>Falha por declaração explícita do tenant — leva o boleto a Extremo Perigo.</summary>
    public bool IsCriticalFailure => Outcome.IsFailure && Severity == CheckSeverity.Critical;

    public bool RequiresAttention => Outcome.RequiresAttention;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Outcome;
        yield return Severity;
        yield return ReasonCode;
        yield return Evidence;
        yield return EvaluatedAt;
    }
}
