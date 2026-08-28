namespace BillPayment.API.BackgroundServices;

/// <summary>Configuração da fila de leitura por IA dos boletos.</summary>
/// <remarks>
/// <strong>Ligada por padrão, e barata quando não há o que fazer.</strong> Sem boleto na fila o
/// ciclo é uma consulta indexada que não devolve nada. Desligar significa boletos nascendo com
/// "Na fila para análise" que nunca sai — o que é pior que a análise não existir.
/// </remarks>
public sealed class BillReadingOptions
{
    public const string SectionName = "BillReading";

    /// <summary>Quando <c>false</c>, o worker não é registrado.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mais folgado que o da captura de propósito: a faixa de visão da captura decide se um
    /// documento vira boleto, e com cota escassa ela tem prioridade sobre o enriquecimento.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Quantos boletos por ciclo. Serial — o teto é a cota do provedor, não o código.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>
    /// Quanto tempo o boleto é de um worker. Longo como o da faixa de visão: a chamada leva
    /// segundos, e um aluguel curto faria outro worker pegar o mesmo boleto no meio da leitura.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Tentativas antes de a análise desistir e virar "indisponível".</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Espera-base entre tentativas. Dobra a cada falha, com teto de 30 minutos.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMinutes(2);
}
