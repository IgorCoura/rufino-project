namespace BillPayment.Infra.DocumentIntelligence;

/// <summary>
/// Configuração do extrator de documentos por IA.
/// </summary>
/// <remarks>
/// <strong><see cref="ApiKey"/> nunca vem de <c>appsettings.json</c></strong> — variável de
/// ambiente em produção, <c>dotnet user-secrets</c> em dev (ADR-009). Sem chave, entra o
/// <see cref="NullDocumentIntelligence"/> e a cascata termina no parser determinístico, sem
/// quebrar nada.
/// </remarks>
public sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    /// <summary>
    /// Qual adapter usar. <c>None</c> (ou vazio) desliga a extração por IA.
    /// </summary>
    public string Provider { get; set; } = "None";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    /// <summary>
    /// Modelo de extração. O default é o mais barato que <em>responde</em>, porque a tarefa é ler
    /// número impresso — não raciocinar.
    /// </summary>
    /// <remarks>
    /// <strong>Estar em <c>GET /models</c> não significa que aceita <c>generateContent</c>.</strong>
    /// Medido em 2026-08-11: a linha <c>gemini-2.5-*</c> aparece na listagem e devolve
    /// <c>404</c> na geração. Ao trocar de modelo, prove com uma chamada real — a listagem mente.
    /// Nome fixo em vez de alias <c>-latest</c> de propósito: alias flutua, e modelo trocando por
    /// baixo faria a qualidade da extração mudar sem nenhuma alteração no repositório.
    /// </remarks>
    public string Model { get; set; } = "gemini-3.1-flash-lite";

    /// <summary>
    /// Teto por chamada. Curto de propósito: o worker processa em série, então uma chamada
    /// pendurada não atrasa um documento — atrasa a fila inteira.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Chamadas por dia, por tenant. <strong>É guarda de conta, não afinação.</strong>
    /// </summary>
    /// <remarks>
    /// Um PDF malformado em laço de retentativa, ou uma caixa antiga recém-conectada com
    /// centenas de anexos, viraria conta surpresa — ou, na conta gratuita, cota queimada num dia
    /// e captura parada no seguinte. O default é conservador de propósito: quem precisa de mais
    /// sobe o número conscientemente.
    /// </remarks>
    public int MaxCallsPerTenantPerDay { get; set; } = 400;

    /// <summary>
    /// <strong>ATENÇÃO: este teto é por TENANT, e o do provedor é por PROJETO.</strong> No Tier 1
    /// pago o limite diário é de 1.000 requisições no projeto inteiro; com três tenants a 400,
    /// a soma estoura. Ao acrescentar cliente, refaça a conta — o provedor não avisa antes,
    /// devolve <c>429</c>, e o artefato vai para a quarentena até o dia seguinte.
    /// </summary>
    public const int PROVIDER_DAILY_CAP_TIER1 = 1_000;

    /// <summary>
    /// Intervalo mínimo entre chamadas, em milissegundos — o limite de taxa do provedor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>600 ms = 100 por minuto, dimensionado para a conta PAGA (Tier 1).</strong> Era
    /// 6.000 ms (10/min, o teto da conta gratuita) até 2026-08-26, e essa espera acontecia
    /// <em>dentro</em> do processamento do artefato — foi medida como a maior parcela dos 20 a 30
    /// segundos que um item de visão levava.
    /// </para>
    /// <para>
    /// O Tier 1 dá 150–300 RPM conforme o modelo. 100/min deixa margem de propósito: o cliente de
    /// visão <strong>não retenta</strong> (cada tentativa consome cota), então um <c>429</c> não
    /// vira nova tentativa — vira artefato na quarentena até o dia seguinte. Chegar perto do teto
    /// troca velocidade por perda.
    /// </para>
    /// <para>
    /// <strong>O número autoritativo é o do painel, não o daqui.</strong> O Google deixou de
    /// publicar os limites por tier na documentação e remete a
    /// <c>aistudio.google.com/rate-limit</c>. Confirme antes de baixar mais.
    /// </para>
    /// </remarks>
    public int MinIntervalMs { get; set; } = 600;

    /// <summary>
    /// Páginas enviadas por documento. Boleto tem uma ou duas; mandar um PDF de 200 páginas
    /// custaria caro para ler um número que está na primeira.
    /// </summary>
    public int MaxPages { get; set; } = 5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.Equals(Provider, "None", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Provider);
}
