namespace BillPayment.API.BackgroundServices;

/// <summary>
/// O ritmo da fila de submissão de pagamentos. A POLÍTICA (24h, janela, tentativas) vive em
/// <c>Payments</c> (<c>PaymentSchedulingOptions</c>, Application) — aqui é só o worker.
/// </summary>
public sealed class PaymentSubmissionOptions
{
    public const string SectionName = "PaymentSubmission";

    /// <summary>
    /// Ligada por padrão, como a fila de leitura: um boleto aprovado que nunca agenda é a tela
    /// mentindo "agendamento em processamento" para sempre. Fora da janela do ADR-017 o worker
    /// acorda, constata e volta a dormir — não submete.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Aluguel de cada ordem reivindicada. Também é o piso do backoff — a mesma coluna.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Quantas ordens retidas por falta de conta reconferir por ciclo.</summary>
    public int AccountHeldProbeBatchSize { get; set; } = 20;
}
