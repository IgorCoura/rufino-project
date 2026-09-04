namespace BillPayment.Domain.Services;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;

/// <summary>
/// Decide a data efetiva de agendamento a partir da pedida, das regras do provedor e da
/// política inicial do ADR-017. Estático e puro: data e hora entram por parâmetro — quem
/// resolve "agora" no fuso do Brasil é o chamador.
/// </summary>
/// <remarks>
/// <para>
/// Duas camadas de regra num lugar só, na ordem em que apertam: as do provedor (piso
/// <c>minimumScheduleDate</c>, dia útil, vencido processa na hora) e as nossas (antecedência
/// mínima e janela de submissão — ADR-017). Se o provedor mudar as dele, muda aqui.
/// </para>
/// <para>
/// A antecedência é conferida contra o <strong>início do expediente</strong> do dia de execução
/// (<see cref="PaymentSchedulingPolicy.SubmissionWindowStart"/>): o provedor não publica a hora
/// em que processa, e medir contra o início do dia erra para o lado da janela de reação maior.
/// Ajuste de dia útil só empurra para frente, então nunca desfaz a antecedência.
/// </para>
/// </remarks>
public static class PaymentSchedulingService
{
    /// <param name="nowLocal">Agora, no fuso do provedor (Brasil) — resolvido pelo chamador.</param>
    public static SchedulingResolution Resolve(
        DateOnly requestedDate,
        DateOnly? dueDate,
        DateOnly? minimumScheduleDate,
        DateTime nowLocal,
        PaymentSchedulingPolicy policy,
        IWorkingDayCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(calendar);

        var today = DateOnly.FromDateTime(nowLocal);

        // Vencido na hora da submissão: o provedor ignora a data e processa imediatamente.
        // Não há data efetiva a calcular — há uma confirmação a exigir (ADR-017).
        if (dueDate is { } due && due < today)
            return SchedulingResolution.Immediate();

        var candidate = requestedDate;

        if (minimumScheduleDate is { } minimum && candidate < minimum)
            candidate = minimum;

        var earliestForLead = EarliestDateHonoringLead(nowLocal, policy);
        if (candidate < earliestForLead)
            candidate = earliestForLead;

        candidate = calendar.NextWorkingDayOnOrAfter(candidate);

        // Se o deslize (piso, antecedência ou dia útil) passou do vencimento, o boleto estará
        // vencido quando o provedor for processar — mas segue AGENDADO, com janela de reação
        // intacta. Encargos são visíveis na tela; imediato é só o já-vencido, acima.
        return SchedulingResolution.Scheduled(candidate);
    }

    /// <summary>
    /// A primeira data cujo início de expediente respeita a antecedência mínima contada de
    /// agora. É daqui que sai o "passa para o dia seguinte" do ADR-017.
    /// </summary>
    private static DateOnly EarliestDateHonoringLead(DateTime nowLocal, PaymentSchedulingPolicy policy)
    {
        var earliestExecution = nowLocal + policy.MinimumLead;
        var date = DateOnly.FromDateTime(earliestExecution);

        return TimeOnly.FromDateTime(earliestExecution) <= policy.SubmissionWindowStart
            ? date
            : date.AddDays(1);
    }
}
