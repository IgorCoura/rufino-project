namespace BillPayment.Domain.PaymentOrders;

/// <summary>
/// O que o agendamento decidiu para uma ordem: uma data efetiva, ou execução imediata — que
/// pelo ADR-017 nunca acontece sem gente confirmando.
/// </summary>
/// <remarks>
/// <c>Immediate</c> nasce de boleto já vencido na hora da submissão: o provedor ignora a data e
/// processa na hora, então não existe janela de reação — e é exatamente por isso que o desfecho
/// é modelado em vez de ser só uma data. Quem consome decide entre submeter (consentido) e reter.
/// </remarks>
public sealed record SchedulingResolution(
    DateOnly? EffectiveDate,
    bool RequiresImmediateExecution)
{
    public bool IsScheduled => EffectiveDate is not null;

    public static SchedulingResolution Scheduled(DateOnly effectiveDate)
        => new(effectiveDate, RequiresImmediateExecution: false);

    public static SchedulingResolution Immediate()
        => new(EffectiveDate: null, RequiresImmediateExecution: true);
}
