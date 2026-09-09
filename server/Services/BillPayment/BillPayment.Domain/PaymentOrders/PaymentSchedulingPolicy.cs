namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A política de agendamento (ADR-017, revista pelo ADR-021) — parâmetro do
/// <c>PaymentSchedulingService</c>, nunca estado da ordem. Os números vêm de configuração; a
/// regra vive no serviço.
/// </summary>
/// <remarks>
/// <para>
/// Sobrou UMA regra: a janela de submissão. Ela restringe o horário em que a fila fala com o
/// provedor — submissão em horário comercial é submissão com gente acordada para reagir a
/// alerta.
/// </para>
/// <para>
/// <strong>A janela é sobre a hora da SUBMISSÃO, não a do pagamento.</strong> Quem decide a hora
/// em que o dinheiro sai é o provedor, e ele só recebe uma data. A consequência prática é que a
/// janela só consegue limitar o pagamento <em>de hoje</em>: fora dela a fila só submeteria na
/// próxima abertura, quando "hoje" já virou ontem. Datas futuras não dependem dela.
/// </para>
/// <para>
/// <strong>A antecedência mínima de 24h saiu em 2026-09-08 (ADR-021).</strong> Não procure por
/// ela: a data pedida hoje é aceita hoje.
/// </para>
/// </remarks>
public sealed class PaymentSchedulingPolicy : ValueObject
{
    public static readonly TimeOnly DEFAULT_WINDOW_START = new(9, 0);
    public static readonly TimeOnly DEFAULT_WINDOW_END = new(18, 0);

    public TimeOnly SubmissionWindowStart { get; }
    public TimeOnly SubmissionWindowEnd { get; }

    private PaymentSchedulingPolicy(TimeOnly windowStart, TimeOnly windowEnd)
    {
        if (windowEnd <= windowStart)
            throw PaymentOrderErrors.SchedulingPolicyInvalid();

        SubmissionWindowStart = windowStart;
        SubmissionWindowEnd = windowEnd;
    }

    public static PaymentSchedulingPolicy Of(TimeOnly windowStart, TimeOnly windowEnd)
        => new(windowStart, windowEnd);

    public static PaymentSchedulingPolicy Default()
        => new(DEFAULT_WINDOW_START, DEFAULT_WINDOW_END);

    /// <summary>A submissão pode acontecer neste horário local?</summary>
    public bool IsWithinSubmissionWindow(TimeOnly localTime)
        => localTime >= SubmissionWindowStart && localTime < SubmissionWindowEnd;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SubmissionWindowStart;
        yield return SubmissionWindowEnd;
    }
}
