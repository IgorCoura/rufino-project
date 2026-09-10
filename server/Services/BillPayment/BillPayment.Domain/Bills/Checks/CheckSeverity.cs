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
    /// Leva o boleto a Extremo Perigo, um degrau acima do <see cref="Blocking"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>São duas famílias, e não uma.</strong> A original é a <em>declaração explícita do
    /// tenant</em> — beneficiário na blacklist, origem bloqueada: alguém aqui dentro já disse que
    /// aquele ator é hostil. A segunda entrou em 2026-09-10 e é a <em>ausência total de
    /// verificação</em>: a consulta oficial não respondeu, então nada foi confirmado sobre o
    /// destino do dinheiro.
    /// </para>
    /// <para>
    /// O que as une é o que elas pedem de quem aprova: alçada máxima e aceite explícito. O que as
    /// separa é o remédio — a primeira se resolve tirando o ator da lista, a segunda se resolve
    /// sozinha quando a consulta volta a responder. Por isso o <strong>motivo</strong> importa
    /// tanto quanto a severidade, e a tela não pode falar de blacklist quando o caso é o outro.
    /// </para>
    /// </remarks>
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
    /// O que fica aqui tem uma forma só: <strong>ausência que não desmente nada</strong>. A
    /// expectativa (conta que ninguém esperava), o prazo (vencido ou fora do corte — problema de
    /// calendário, não de fraude), o nome do beneficiário (grafia divergente ou cotejo só por
    /// nome) e, desde 2026-09-10, a <strong>ausência de cadastro</strong>: beneficiário sem bancos
    /// aceitos, sem política de valor, boleto atribuído por inferência, e leitura sem campo oficial
    /// correspondente para confrontar.
    /// </para>
    /// <para>
    /// <strong>A fronteira é entre ausência de CADASTRO e ausência de IDENTIDADE.</strong> Não ter
    /// declarado bancos aceitos é o tenant não ter configurado nada; não saber quem é o
    /// beneficiário (<c>payee_not_registered</c>) ou de quem é o boleto
    /// (<c>payer_not_extractable</c>) é o sistema não ter provado nada — e esses continuam
    /// pesando como Perigo. Contradição entre fontes nunca chega aqui.
    /// </para>
    /// </remarks>
    public static readonly CheckSeverity Notice = new(4, nameof(Notice));

    private CheckSeverity(int id, string name) : base(id, name) { }
}
