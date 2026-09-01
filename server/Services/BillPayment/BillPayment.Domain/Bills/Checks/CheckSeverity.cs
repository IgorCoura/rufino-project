namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Quanto pesa a falha de uma verificação.
/// </summary>
/// <remarks>
/// Severidade é <strong>separada do resultado</strong> de propósito (ADR-003): permite calibrar
/// o rigor de um check sem reescrever como ele apura. Um mesmo <c>Failed</c> reprova o boleto
/// quando o check é <see cref="Blocking"/> e só chama atenção quando é <see cref="Advisory"/>.
/// </remarks>
public sealed class CheckSeverity : Enumeration
{
    /// <summary>Falha impede a aprovação até o motivo ser resolvido.</summary>
    public static readonly CheckSeverity Blocking = new(1, nameof(Blocking));

    /// <summary>Falha é destacada na tela; o aprovador pode autorizar assumindo o risco, com o motivo gravado.</summary>
    public static readonly CheckSeverity Advisory = new(2, nameof(Advisory));

    /// <summary>
    /// Falha por declaração explícita do tenant (blacklist, origem bloqueada) — leva o boleto a
    /// Extremo Perigo, um degrau acima do <see cref="Blocking"/>.
    /// </summary>
    public static readonly CheckSeverity Critical = new(3, nameof(Critical));

    private CheckSeverity(int id, string name) : base(id, name) { }
}
