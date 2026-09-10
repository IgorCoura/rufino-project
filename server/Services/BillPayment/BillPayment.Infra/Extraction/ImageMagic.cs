namespace BillPayment.Infra.Extraction;

/// <summary>
/// Reconhece imagem pelos bytes, não pelo que o servidor diz que ela é.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque o <c>Content-Type</c> é declaração de quem serve, e quem serve está fora
/// do nosso controle.</strong> O PDF sempre foi conferido pelo <c>%PDF-</c> no byte zero; a
/// imagem era aceita pelo cabeçalho, e a assimetria abria a porta que o PDF fechava — um servidor
/// hostil respondendo <c>image/png</c> com vinte megabytes de qualquer coisa entregava esses bytes
/// ao decodificador de imagem do leitor de QR, ao extrator de IA e ao aparelho de quem aprova.
/// </para>
/// <para>
/// <strong>Só os três formatos que a cascata abre.</strong> SVG de propósito fora: é documento
/// XML executável, não imagem rasterizada, e nenhum boleto medido chega assim.
/// </para>
/// </remarks>
internal static class ImageMagic
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];

    private static readonly byte[] Riff = "RIFF"u8.ToArray();

    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <summary>
    /// O tipo de mídia que os bytes de fato carregam, ou <c>null</c> quando não é imagem aceita.
    /// </summary>
    public static string? MediaTypeOf(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(Png))
            return "image/png";

        if (content.StartsWith(Jpeg))
            return "image/jpeg";

        // WEBP é um contêiner RIFF: "RIFF" nos bytes 0-3, tamanho nos 4-7, "WEBP" nos 8-11.
        if (content.Length >= 12 && content.StartsWith(Riff) && content[8..12].SequenceEqual(Webp))
            return "image/webp";

        return null;
    }
}
