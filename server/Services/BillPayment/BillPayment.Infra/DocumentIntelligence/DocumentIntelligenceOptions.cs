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
    /// Modelo de extração. O default é o mais barato da linha, porque a tarefa é ler número
    /// impresso — não raciocinar — e porque a conta free tem cota diária apertada.
    /// </summary>
    public string Model { get; set; } = "gemini-2.5-flash-lite";

    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Chamadas por dia, por tenant. <strong>É guarda de conta, não afinação.</strong>
    /// </summary>
    /// <remarks>
    /// Um PDF malformado em laço de retentativa, ou uma caixa antiga recém-conectada com
    /// centenas de anexos, viraria conta surpresa — ou, na conta gratuita, cota queimada num dia
    /// e captura parada no seguinte. O default é conservador de propósito: quem precisa de mais
    /// sobe o número conscientemente.
    /// </remarks>
    public int MaxCallsPerTenantPerDay { get; set; } = 100;

    /// <summary>
    /// Intervalo mínimo entre chamadas, em milissegundos — o limite de taxa do provedor.
    /// </summary>
    /// <remarks>
    /// 6 segundos = 10 por minuto, que é o teto típico da conta gratuita. Estourar não devolve
    /// erro útil: devolve <c>429</c>, e retentar em cima piora. Segurar aqui é mais barato do que
    /// descobrir do outro lado.
    /// </remarks>
    public int MinIntervalMs { get; set; } = 6000;

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
