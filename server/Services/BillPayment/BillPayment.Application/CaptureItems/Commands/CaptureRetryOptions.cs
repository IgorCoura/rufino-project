namespace BillPayment.Application.CaptureItems.Commands;

/// <summary>
/// Quantas vezes insistir num artefato, e quanto esperar entre as tentativas.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É política, não infraestrutura</strong> — mora na Application junto do caso de uso
/// que a consome, como <c>ApprovalOptions</c>. O worker escolhe o ritmo dos ciclos; quantas
/// tentativas um documento merece antes de virar caso para uma pessoa é decisão de negócio.
/// </para>
/// <para>
/// <strong>Os defaults saem da literatura de fila, não de chute</strong>: começar conservador
/// (2–3 tentativas), com espera que dobra, e só subir o teto para filas onde a medição mostre
/// que a maioria das falhas se resolve repetindo. Aqui o desperdício de insistir é alto —
/// baixar megabytes do provedor e reler PDF — e o custo de desistir é baixo, porque o item
/// fica visível em <c>Failed</c> e reabre por um clique.
/// </para>
/// </remarks>
public sealed class CaptureRetryOptions
{
    public const string SectionName = "CaptureRetry";

    /// <summary>Tentativas antes de o artefato virar caso para uma pessoa.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Espera após a primeira falha. Dobra a cada falha seguinte, até o teto do agregado.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Quanto tempo um worker segura um artefato da faixa rápida antes de o aluguel vencer.
    /// </summary>
    /// <remarks>
    /// Tem de ser folgadamente maior que o processamento real (mediana de 150 ms, máximo medido
    /// de ~2 s) — se vencer antes de o worker terminar, outro pega o mesmo item e os dois
    /// trabalham em cima dele.
    /// </remarks>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// O mesmo para a faixa de visão, onde uma chamada leva de 3 a 5 s e pode esperar cota.
    /// </summary>
    public TimeSpan VisionLeaseDuration { get; set; } = TimeSpan.FromMinutes(15);
}
