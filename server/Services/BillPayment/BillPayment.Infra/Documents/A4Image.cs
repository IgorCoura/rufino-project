namespace BillPayment.Infra.Documents;

using SkiaSharp;

/// <summary>
/// Prepara uma imagem para ocupar uma folha A4: rotação do EXIF aplicada, resolução de impressão
/// e JPEG.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A foto entrava no PDF na resolução do celular.</strong> Medido em 2026-09-14 com uma foto
/// de 12 MP: o PNG virava 34 MB de PDF (o PdfPig embute PNG como pixels comprimidos, sem perda) e o
/// JPEG, 3,7 MB — para uma página que ninguém imprime acima de 200 DPI. Normalizada, a mesma foto dá
/// 270 KB, e o código de barras continua legível.
/// </para>
/// <para>
/// <strong>A folha acompanha a foto.</strong> Boleto fotografado costuma sair deitado; em A4 em pé
/// ele ficaria com um terço da largura útil. Imagem mais larga que alta vai em A4 deitada.
/// </para>
/// <para>
/// <strong>A rotação vem do EXIF.</strong> O celular grava os pixels na orientação do sensor e diz
/// em metadado como exibir; decodificar sem aplicá-la põe o boleto de lado no PDF.
/// </para>
/// </remarks>
internal static class A4Image
{
    public const double A4_SHORT_SIDE = 595;
    public const double A4_LONG_SIDE = 842;
    public const double MARGIN = 48;

    /// <summary>Resolução de impressão: o código de barras lê com folga, e o arquivo fica pequeno.</summary>
    public const int TARGET_DPI = 200;

    public const int JPEG_QUALITY = 82;

    /// <summary>Acima disto a imagem é tratada como ilegível — bomba de descompressão.</summary>
    public const long MAX_SOURCE_PIXELS = 60_000_000;

    private const double POINTS_PER_INCH = 72;

    /// <summary>A imagem pronta para a página, ou <c>null</c> quando ela não pôde ser lida.</summary>
    public static A4ImagePage? Fit(byte[] content)
    {
        using var codec = SKCodec.Create(new MemoryStream(content));
        if (codec is null)
            return null;

        // Dimensões pelo cabeçalho real, antes de qualquer decodificação.
        var width = codec.Info.Width;
        var height = codec.Info.Height;
        if (width <= 0 || height <= 0 || (long)width * height > MAX_SOURCE_PIXELS)
            return null;

        var origin = codec.EncodedOrigin;
        var swapsSides = SwapsSides(origin);
        var shownWidth = swapsSides ? height : width;
        var shownHeight = swapsSides ? width : height;

        var landscape = shownWidth > shownHeight;
        var (areaWidth, areaHeight) = PrintableArea(landscape);
        var scale = Math.Min(
            1.0,
            Math.Min(
                areaWidth / POINTS_PER_INCH * TARGET_DPI / shownWidth,
                areaHeight / POINTS_PER_INCH * TARGET_DPI / shownHeight));

        using var decoded = Decode(codec, scale);
        if (decoded is null)
            return null;

        var targetWidth = Math.Max(1, (int)Math.Round(shownWidth * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(shownHeight * scale));

        using var surface = SKSurface.Create(
            new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        // Fundo branco: JPEG não tem transparência, e PNG transparente viraria preto.
        canvas.Clear(SKColors.White);

        var decodedShownWidth = swapsSides ? decoded.Height : decoded.Width;
        var decodedShownHeight = swapsSides ? decoded.Width : decoded.Height;
        canvas.Scale((float)targetWidth / decodedShownWidth, (float)targetHeight / decodedShownHeight);
        Orient(canvas, origin, decodedShownWidth, decodedShownHeight);

        using (var image = SKImage.FromBitmap(decoded))
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));

        using var snapshot = surface.Snapshot();
        using var jpeg = snapshot.Encode(SKEncodedImageFormat.Jpeg, JPEG_QUALITY);

        return new A4ImagePage(jpeg.ToArray(), targetWidth, targetHeight, landscape);
    }

    /// <summary>A área útil da folha, em pontos, já sem as margens.</summary>
    public static (double Width, double Height) PrintableArea(bool landscape)
        => landscape
            ? (A4_LONG_SIDE - (2 * MARGIN), A4_SHORT_SIDE - (2 * MARGIN))
            : (A4_SHORT_SIDE - (2 * MARGIN), A4_LONG_SIDE - (2 * MARGIN));

    /// <summary>
    /// Decodifica já reduzida quando o formato permite (JPEG reduz no próprio decodificador), com
    /// folga de 2× para a redução final ter qualidade.
    /// </summary>
    private static SKBitmap? Decode(SKCodec codec, double scale)
    {
        var size = codec.GetScaledDimensions((float)Math.Min(1.0, scale * 2));
        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());

        if (result is SKCodecResult.Success or SKCodecResult.IncompleteInput)
            return bitmap;

        bitmap.Dispose();
        return null;
    }

    private static bool SwapsSides(SKEncodedOrigin origin)
        => origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    /// <summary>
    /// A transformação que leva os pixels como o sensor os gravou para como a foto deve ser vista.
    /// <paramref name="width"/> e <paramref name="height"/> são as dimensões JÁ na orientação final.
    /// </summary>
    private static void Orient(SKCanvas canvas, SKEncodedOrigin origin, float width, float height)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(width, height);
                canvas.RotateDegrees(270);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }
    }
}

/// <summary>A imagem pronta: JPEG, dimensões em pixels e a orientação da folha.</summary>
internal sealed record A4ImagePage(byte[] Jpeg, int PixelWidth, int PixelHeight, bool Landscape);
