namespace BillPayment.API.BackgroundServices;

/// <summary>
/// Configuração do agendador de sincronização de caixas.
/// </summary>
/// <remarks>
/// <strong>Desligado por padrão</strong>, ao contrário do outbox. Enquanto não existe adapter de
/// provedor (o Graph entra na 2.2), o worker só produziria falhas registradas de minuto em
/// minuto em toda fonte cadastrada — ruído que esconderia a falha de verdade quando ela viesse.
/// </remarks>
public sealed class CaptureSyncOptions
{
    public const string SectionName = "Capture";

    /// <summary>
    /// Quando <c>false</c>, o worker não é registrado. Também é o que permite um único
    /// deployment varrer as caixas quando a API escalar horizontalmente.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Caixa de contas a pagar tem volume baixo e o provedor cobra chamada com throttling —
    /// varrer de minuto em minuto é frequente o bastante e barato.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Quantas fontes por ciclo. Cada uma roda na sua própria transação.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Ritmo do processamento de artefatos, separado do da varredura.
    /// </summary>
    /// <remarks>
    /// Mais curto que o da varredura porque aqui há fila de verdade: assim que a sincronização
    /// ingere, o boleto deve chegar à tela de aprovação sem esperar o próximo minuto cheio.
    /// </remarks>
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Artefatos por ciclo. Menor que o lote de fontes porque cada um baixa bytes e roda
    /// extração — um lote grande seguraria memória sem entregar mais rápido.
    /// </summary>
    public int ProcessingBatchSize { get; set; } = 10;
}
