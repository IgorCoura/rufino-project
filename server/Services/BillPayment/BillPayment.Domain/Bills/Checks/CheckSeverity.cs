namespace BillPayment.Domain.Bills.Checks;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Quanto pesa a falha de uma verificação.
/// </summary>
/// <remarks>
/// <para>
/// Severidade é <strong>separada do resultado</strong> de propósito (ADR-003): permite calibrar
/// o rigor de um check sem reescrever como ele apura. É ela, e não o resultado, que diz quanto
/// um desfecho pesa na classificação de risco — ver <c>CheckResult.RiskContribution</c>.
/// </para>
/// <para>
/// <strong>São quatro degraus desde 2026-09-08</strong>, e desde então
/// <see cref="Blocking"/> e <see cref="Advisory"/> chegam ao mesmo lugar: Perigo. O que os
/// separa é só a intenção documental — <c>Advisory</c> marca o check cuja falha é interpretável,
/// <c>Blocking</c> o que não é. Quem quiser teto de Atenção usa <see cref="Notice"/>.
/// </para>
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

    /// <summary>
    /// O degrau <strong>abaixo</strong> de <see cref="Advisory"/>: o desfecho é destacado, mas
    /// seu teto é <c>Atenção</c> — nunca leva o boleto a Perigo, qualquer que seja o resultado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Entrou em 2026-09-08, junto com a régua que promoveu a Perigo tudo que antes era Atenção
    /// (ADR-020). Sem ele a promoção seria cega: <c>Inconclusive</c> é o desfecho mais comum do
    /// catálogo, e mandar todos para Perigo tornaria o "assumo o risco" rotina — que é como o
    /// alerta que importa passa batido, exatamente o que o ADR-003 mandou evitar.
    /// </para>
    /// <para>
    /// Três coisas ficam aqui, e só elas: <strong>a expectativa</strong> (conta que ninguém
    /// esperava), <strong>o prazo</strong> (vencido, fora do corte — problema de calendário, não
    /// de fraude) e <strong>o nome do beneficiário</strong> (grafia divergente ou cotejo só por
    /// nome, com a identidade confirmada em parte). Contradição entre fontes nunca chega aqui.
    /// </para>
    /// </remarks>
    public static readonly CheckSeverity Notice = new(4, nameof(Notice));

    private CheckSeverity(int id, string name) : base(id, name) { }
}
