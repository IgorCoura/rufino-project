namespace BillPayment.Domain.Services;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;

/// <summary>
/// Decide a data efetiva de agendamento a partir da pedida, das regras do provedor e da política
/// do ADR-017 revista pelo ADR-021. Estático e puro: data e hora entram por parâmetro — quem
/// resolve "agora" no fuso do Brasil é o chamador.
/// </summary>
/// <remarks>
/// <para>
/// Duas camadas de regra num lugar só, na ordem em que apertam: as do provedor (piso
/// <c>minimumScheduleDate</c>, dia útil, vencido processa na hora) e a nossa — que hoje se
/// resume à janela de submissão, e só através do <see cref="CanScheduleForToday"/>.
/// </para>
/// <para>
/// <strong>A antecedência mínima de 24h foi removida em 2026-09-08 (ADR-021).</strong> Ela
/// empurrava a data pedida para frente; agora o piso da data é simplesmente <em>hoje</em>. O que
/// sobrou da janela não mexe em data nenhuma: ela decide se HOJE ainda é uma escolha possível, e
/// nada mais.
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

        // Piso temporal: a ordem pode ter esperado na fila (janela fechada, retenção, worker
        // parado) e amanhecido com a data pedida no passado. Submeter data vencida seria recusa
        // do provedor; deslizar para hoje é o que a pessoa quis dizer.
        if (candidate < today)
            candidate = today;

        candidate = calendar.NextWorkingDayOnOrAfter(candidate);

        // Se o deslize (piso ou dia útil) passou do vencimento, o boleto estará vencido quando o
        // provedor for processar — mas segue AGENDADO, com a possibilidade de cancelar intacta.
        // Encargos são visíveis na tela; imediato é só o já-vencido, acima.
        return SchedulingResolution.Scheduled(candidate);
    }

    /// <summary>
    /// Hoje ainda serve como data de pagamento, neste instante? É a única regra da política que
    /// olha para a hora — e a única que a tela precisa consultar antes de oferecer "pagar hoje".
    /// </summary>
    /// <remarks>
    /// A ordem das perguntas é a das consequências. A janela vem primeiro porque é a única que
    /// vale mesmo para boleto vencido: fora dela não há submissão nenhuma hoje, ponto. Vencido
    /// dispensa o resto — o provedor paga na hora, ignorando piso e calendário —, e por isso a
    /// checagem de dia útil não pode vir antes dele: negaria o pagamento de um vencido num
    /// sábado, que é justamente quando ele funcionaria.
    /// </remarks>
    public static SameDayScheduling CanScheduleForToday(
        DateOnly? dueDate,
        DateOnly? minimumScheduleDate,
        DateTime nowLocal,
        PaymentSchedulingPolicy policy,
        IWorkingDayCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(calendar);

        if (!policy.IsWithinSubmissionWindow(TimeOnly.FromDateTime(nowLocal)))
            return SameDayScheduling.Deny(SameDayScheduling.OUTSIDE_WINDOW);

        var today = DateOnly.FromDateTime(nowLocal);

        if (dueDate is { } due && due < today)
            return SameDayScheduling.Allow();

        if (minimumScheduleDate is { } minimum && today < minimum)
            return SameDayScheduling.Deny(SameDayScheduling.PROVIDER_MINIMUM);

        if (!calendar.IsWorkingDay(today))
            return SameDayScheduling.Deny(SameDayScheduling.NOT_A_WORKING_DAY);

        return SameDayScheduling.Allow();
    }
}
