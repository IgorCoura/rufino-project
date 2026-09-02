namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A política inicial de agendamento do ADR-017 — parâmetro do <c>PaymentSchedulingService</c>,
/// nunca estado da ordem. Os números vêm de configuração; a regra vive no serviço.
/// </summary>
/// <remarks>
/// <see cref="MinimumLead"/> é a janela de reação: entre submeter e o dinheiro sair há tempo de
/// cancelar. <see cref="SubmissionWindowStart"/>/<see cref="SubmissionWindowEnd"/> restringem a
/// submissão a horário com gente acordada para reagir a alerta. A antecedência é medida contra o
/// <strong>início do expediente do dia de execução</strong> (o próprio
/// <see cref="SubmissionWindowStart"/>) — o provedor não publica a hora em que processa, e medir
/// contra o início do dia é errar para o lado da janela maior.
/// </remarks>
public sealed class PaymentSchedulingPolicy : ValueObject
{
    public static readonly TimeSpan DEFAULT_MINIMUM_LEAD = TimeSpan.FromHours(24);
    public static readonly TimeOnly DEFAULT_WINDOW_START = new(9, 0);
    public static readonly TimeOnly DEFAULT_WINDOW_END = new(17, 0);

    public TimeSpan MinimumLead { get; }
    public TimeOnly SubmissionWindowStart { get; }
    public TimeOnly SubmissionWindowEnd { get; }

    private PaymentSchedulingPolicy(TimeSpan minimumLead, TimeOnly windowStart, TimeOnly windowEnd)
    {
        if (minimumLead < TimeSpan.Zero || windowEnd <= windowStart)
            throw PaymentOrderErrors.SchedulingPolicyInvalid();

        MinimumLead = minimumLead;
        SubmissionWindowStart = windowStart;
        SubmissionWindowEnd = windowEnd;
    }

    public static PaymentSchedulingPolicy Of(TimeSpan minimumLead, TimeOnly windowStart, TimeOnly windowEnd)
        => new(minimumLead, windowStart, windowEnd);

    public static PaymentSchedulingPolicy Default()
        => new(DEFAULT_MINIMUM_LEAD, DEFAULT_WINDOW_START, DEFAULT_WINDOW_END);

    /// <summary>A submissão pode acontecer neste horário local?</summary>
    public bool IsWithinSubmissionWindow(TimeOnly localTime)
        => localTime >= SubmissionWindowStart && localTime < SubmissionWindowEnd;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinimumLead;
        yield return SubmissionWindowStart;
        yield return SubmissionWindowEnd;
    }
}
