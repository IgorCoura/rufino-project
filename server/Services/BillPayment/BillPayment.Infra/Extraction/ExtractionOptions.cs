namespace BillPayment.Infra.Extraction;

/// <summary>
/// Limites da cascata de extração. São <strong>guardas contra documento hostil</strong>, não
/// afinação de desempenho.
/// </summary>
public sealed class ExtractionOptions
{
    public const string SectionName = "Extraction";

    /// <summary>
    /// Teto de senhas derivadas por documento (doc 09). Isto é <em>derivação</em>, não força
    /// bruta: as candidatas saem de dados que o próprio tenant cadastrou, e o teto existe para
    /// que um PDF hostil não vire um laço caro.
    /// </summary>
    public int MaxPasswordCandidates { get; set; } = 40;

    /// <summary>
    /// Páginas lidas por documento. Boleto tem uma ou duas; um PDF de mil páginas é outra coisa,
    /// e varrê-lo inteiro custaria caro para nunca virar boleto.
    /// </summary>
    public int MaxPages { get; set; } = 20;
}
