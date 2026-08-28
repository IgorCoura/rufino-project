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
    /// Espera entre varreduras quando a anterior parou no teto de páginas em vez de chegar ao
    /// fim da caixa.
    /// </summary>
    /// <remarks>
    /// <strong>Existe porque a mensagem nova está no FIM da enumeração.</strong> A delta query do
    /// provedor vai do mais antigo para o mais novo e devolve um teto de páginas por chamada;
    /// dormir o <see cref="PollingInterval"/> inteiro sobre uma varredura truncada deixa o e-mail
    /// que acabou de chegar horas fora de alcance. Medido em 2026-08-26 na caixa real: 12.422
    /// mensagens, 1.000 por varredura — treze varreduras até o topo. Curto, mas não zero: sem
    /// pausa nenhuma o agendador viraria laço apertado contra o provedor e cairia em limitação
    /// de taxa.
    /// </remarks>
    public TimeSpan CatchUpInterval { get; set; } = TimeSpan.FromSeconds(5);

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

    /// <summary>
    /// Quantos artefatos da faixa rápida são processados ao mesmo tempo.
    /// </summary>
    /// <remarks>
    /// <strong>Paralelizar aqui compensa porque este trabalho é I/O.</strong> Baixar do provedor,
    /// gravar no balde e ler o PDF passam a maior parte do tempo esperando rede e disco — medido
    /// em 2026-08-26: mediana de 150 ms por artefato, quase toda em espera. O teto existe para não
    /// virar enxurrada contra o provedor de e-mail e o balde, que são compartilhados.
    /// <para>
    /// <strong>Não vale para a faixa de visão</strong>, que é serial de propósito: lá o teto é a
    /// cota da conta de IA, e concorrência só troca espera por <c>429</c>.
    /// </para>
    /// </remarks>
    public int ProcessingConcurrency { get; set; } = 4;

    /// <summary>Artefatos por ciclo da fila de visão. Pequeno: cada um custa cota e segundos.</summary>
    public int VisionBatchSize { get; set; } = 5;

    /// <summary>Espera entre ciclos da fila de visão quando ela ficou vazia.</summary>
    public TimeSpan VisionInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Espera quando o lote de visão saiu cheio — há mais fila, emenda o próximo.</summary>
    public TimeSpan VisionCatchUpInterval { get; set; } = TimeSpan.FromSeconds(2);
}
