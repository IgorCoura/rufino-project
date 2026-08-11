namespace BillPayment.Infra.Mailboxes.Graph;

/// <summary>
/// Configuração do adapter de caixa do Microsoft Graph.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não há segredo aqui.</strong> A credencial é <em>por fonte</em> e vive cifrada no
/// cofre — cada cliente registra o próprio aplicativo no Entra ID dele (ADR-006: o registro é
/// autosserviço, o usuário é admin do seu tenant). Estas opções são só endereços e limites.
/// </para>
/// </remarks>
public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    /// <summary>
    /// Desligado por padrão: sem isto explícito, o BC usa o <c>UnconfiguredMailboxReader</c> e
    /// nenhuma fonte conecta. Ligar é decisão de quem configura o ambiente.
    /// </summary>
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0/";

    public string LoginUrl { get; set; } = "https://login.microsoftonline.com/";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Mensagens por página da delta query.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Teto de páginas por varredura. Existe para a primeira sincronização de uma caixa antiga
    /// não virar um laço de horas — o que sobrar volta no ciclo seguinte, porque o
    /// <c>@odata.deltaLink</c> só aparece na última página e sem ele o cursor não avança.
    /// </summary>
    public int MaxPagesPerSync { get; set; } = 20;

    /// <summary>
    /// Anexo maior que isto é ignorado. Boleto é documento de poucos KB; arquivo grande é vídeo,
    /// apresentação ou backup, e baixá-lo custaria caro para nunca virar boleto.
    /// </summary>
    public long MaxAttachmentBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Tipos que podem carregar boleto. O que não estiver aqui nem é ingerido — evita encher a
    /// fila com <c>.ics</c>, <c>.vcf</c> e assinaturas.
    /// </summary>
    public IList<string> AllowedContentTypes { get; } =
    [
        "application/pdf",
        "application/octet-stream",
        "image/png",
        "image/jpeg",
    ];
}
