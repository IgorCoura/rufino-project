namespace BillPayment.API.BackgroundServices;

/// <summary>Configuração do agendador de expectativas.</summary>
/// <remarks>
/// <strong>Ligado por padrão, ao contrário da captura.</strong> A captura desligada apenas não
/// captura; a expectativa desligada <em>desliga a rede de segurança</em> — e o modo de falha dela
/// é o silêncio, que é exatamente o que o ADR-014 existe para evitar. Sem expectativa cadastrada
/// o ciclo não faz nada e não custa nada.
/// </remarks>
public sealed class ExpectationSweepOptions
{
    public const string SectionName = "Expectations";

    /// <summary>
    /// Quando <c>false</c>, o worker não é registrado — e é o que permite um único deployment
    /// varrer as expectativas quando a API escalar horizontalmente.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// O que este worker observa muda de dia em dia, não de minuto em minuto. Seis horas dá
    /// quatro chances por dia de alcançar a virada de data em qualquer fuso.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Quantas expectativas por ciclo. Cada uma roda na sua própria transação.</summary>
    public int BatchSize { get; set; } = 100;
}
