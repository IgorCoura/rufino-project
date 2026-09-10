namespace BillPayment.Domain.Extraction;

/// <summary>
/// O tipo de mídia lido nos <strong>bytes</strong> do documento, não no rótulo de quem o entregou.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque o rótulo é declaração de terceiro, e terceiro erra.</strong> O emissor
/// do boleto da Notredame Intermédica manda o anexo com <c>Content-Type: pdf</c> — que não é um
/// tipo de mídia válido, porque lhe falta o <c>application/</c>. O Exchange normaliza o que não
/// entende para <c>application/octet-stream</c>, e era esse rótulo que atravessava a ingestão, o
/// balde e a API até a tela, onde o app dizia não saber exibir um PDF perfeitamente legível.
/// </para>
/// <para>
/// <strong>É o mesmo defeito de 2026-08-11, uma camada adiante.</strong> Lá o tipo era deduzido
/// da extensão de uma chave opaca e todo anexo virava PDF; a correção foi guardar o que o
/// provedor declarou. Só que guardar a declaração não a torna verdadeira — quem sabe o que o
/// arquivo é são os primeiros bytes dele, e é isso que este tipo lê.
/// </para>
/// <para>
/// <strong>Só os quatro formatos que a cascata abre.</strong> SVG fica de fora de propósito: é
/// documento XML executável, não imagem rasterizada, e nenhum boleto medido chega assim.
/// </para>
/// </remarks>
public static class DocumentMagic
{
    /// <summary>
    /// Quantos bytes bastam para decidir.
    /// </summary>
    /// <remarks>
    /// O WEBP é o mais longo dos quatro: <c>"RIFF"</c> nos bytes 0-3, o tamanho nos 4-7 e
    /// <c>"WEBP"</c> nos 8-11. Existe como constante para quem serve o documento em fluxo poder
    /// espiar exatamente o necessário antes de repassar o resto.
    /// </remarks>
    public const int PREFIX_LENGTH = 12;

    private static ReadOnlySpan<byte> Pdf => "%PDF-"u8;

    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> Riff => "RIFF"u8;

    private static ReadOnlySpan<byte> Webp => "WEBP"u8;

    /// <summary>
    /// O tipo de mídia que os bytes de fato carregam, ou <c>null</c> quando nenhuma assinatura
    /// conhecida está ali.
    /// </summary>
    /// <remarks>
    /// <strong>Nulo não quer dizer "arquivo ruim".</strong> Corpo de e-mail em HTML, texto puro e
    /// PDF com lixo antes do <c>%PDF-</c> caem aqui — e para todos eles a resposta certa é deixar
    /// o rótulo declarado valer, não recusar o documento.
    /// </remarks>
    public static string? MediaTypeOf(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(Pdf))
            return "application/pdf";

        if (content.StartsWith(Png))
            return "image/png";

        if (content.StartsWith(Jpeg))
            return "image/jpeg";

        if (content.Length >= PREFIX_LENGTH && content.StartsWith(Riff) && content[8..12].SequenceEqual(Webp))
            return "image/webp";

        return null;
    }
}
