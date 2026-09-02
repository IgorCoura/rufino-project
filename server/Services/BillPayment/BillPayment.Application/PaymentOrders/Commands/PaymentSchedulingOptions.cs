namespace BillPayment.Application.PaymentOrders.Commands;

using System.Globalization;
using BillPayment.Domain.PaymentOrders;

/// <summary>
/// A política inicial de agendamento (ADR-017) e os parâmetros da fila de submissão, como
/// configuração da instalação. Afrouxar a política é mudar números aqui + revisar o ADR —
/// nunca reescrever regra, que vive no <c>PaymentSchedulingService</c>.
/// </summary>
public sealed class PaymentSchedulingOptions
{
    public const string SectionName = "Payments";

    /// <summary>Antecedência mínima entre a submissão e a data efetiva — a janela de reação.</summary>
    public int MinLeadHours { get; set; } = 24;

    /// <summary>Janela de submissão, no fuso do provedor. Fora dela a fila espera.</summary>
    public string SubmissionWindowStart { get; set; } = "09:00";

    public string SubmissionWindowEnd { get; set; } = "17:00";

    /// <summary>
    /// O fuso do provedor. IANA funciona em Windows e Linux desde o .NET 6 (ICU); o fallback
    /// cobre a máquina Windows sem ICU.
    /// </summary>
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";

    private const string WINDOWS_FALLBACK_TIME_ZONE_ID = "E. South America Standard Time";

    /// <summary>Teto de tentativas de submissão antes de a ordem desistir com falha visível.</summary>
    public int MaxSubmissionAttempts { get; set; } = 5;

    /// <summary>Base da espera entre tentativas — dobra a cada falha, teto no agregado.</summary>
    public int RetryBaseDelaySeconds { get; set; } = 30;

    public PaymentSchedulingPolicy ToPolicy()
        => PaymentSchedulingPolicy.Of(
            TimeSpan.FromHours(MinLeadHours < 0 ? 0 : MinLeadHours),
            ParseTime(SubmissionWindowStart, PaymentSchedulingPolicy.DEFAULT_WINDOW_START),
            ParseTime(SubmissionWindowEnd, PaymentSchedulingPolicy.DEFAULT_WINDOW_END));

    public TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WINDOWS_FALLBACK_TIME_ZONE_ID);
        }
    }

    private static TimeOnly ParseTime(string? raw, TimeOnly fallback)
        => TimeOnly.TryParse(raw, CultureInfo.InvariantCulture, out var value) ? value : fallback;
}
