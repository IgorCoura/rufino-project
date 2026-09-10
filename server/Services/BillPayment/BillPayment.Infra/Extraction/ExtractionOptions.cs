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
    /// <remarks>
    /// <strong>Subiu de 40 para 60 em 2026-09-10, junto com os prefixos de 3 e 4 dígitos.</strong>
    /// A lista é montada percorrendo cada documento fiscal com todos os prefixos, então dois
    /// prefixos novos por documento aproximam o teto — e quem fica de fora é o fim da fila, que é
    /// justamente onde vivem os documentos adicionais. A medição de 2026-08-11 mostrou 7 de 11
    /// PDFs cifrados abrindo por documento <em>adicional</em>: truncar ali custaria documento que
    /// hoje abre.
    /// </remarks>
    public int MaxPasswordCandidates { get; set; } = 60;

    /// <summary>
    /// Páginas lidas por documento. Boleto tem uma ou duas; um PDF de mil páginas é outra coisa,
    /// e varrê-lo inteiro custaria caro para nunca virar boleto.
    /// </summary>
    public int MaxPages { get; set; } = 20;
}
