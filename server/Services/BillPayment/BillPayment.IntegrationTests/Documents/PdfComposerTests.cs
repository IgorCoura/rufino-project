namespace BillPayment.IntegrationTests.Documents;

using BillPayment.Domain.Ports;
using BillPayment.Infra.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O montador de PDF da exportação de documentos, sem banco e sem HTTP.
/// </summary>
public sealed class PdfComposerTests
{
    private static readonly PdfComposer Composer = new(NullLogger<PdfComposer>.Instance);

    // Documentos entram na ordem em que foram acrescentados, com todas as páginas.
    [Fact]
    public void TryAppend_WithTwoPdfs_ShouldKeepEveryPageInOrder()
    {
        using var composition = Composer.Begin();

        Assert.True(composition.TryAppend(PdfWithPages("A", 2), "application/pdf"));
        Assert.True(composition.TryAppend(PdfWithPages("B", 3), "application/pdf"));

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(5, document.NumberOfPages);
        Assert.Contains("A-1", document.GetPage(1).Text, StringComparison.Ordinal);
        Assert.Contains("B-1", document.GetPage(3).Text, StringComparison.Ordinal);
        Assert.Contains("B-3", document.GetPage(5).Text, StringComparison.Ordinal);
    }

    // O teto de páginas corta o documento no começo — é a opção "somente a primeira página".
    [Fact]
    public void TryAppend_WithMaxPages_ShouldKeepOnlyTheFirstPages()
    {
        using var composition = Composer.Begin();

        composition.TryAppend(PdfWithPages("A", 4), "application/pdf", maxPages: 1);

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(1, document.NumberOfPages);
        Assert.Contains("A-1", document.GetPage(1).Text, StringComparison.Ordinal);
    }

    // Imagem vira uma página — inclusive WEBP, que o PDF não sabe embutir e é reencodado.
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void TryAppend_WithAnImage_ShouldProduceOnePage(SKEncodedImageFormat format)
    {
        using var composition = Composer.Begin();

        Assert.True(composition.TryAppend(ImageBytes(format), "application/octet-stream"));

        using var document = PdfDocument.Open(composition.Build());
        Assert.Equal(1, document.NumberOfPages);
        Assert.Single(document.GetPage(1).GetImages());
    }

    // Teste de regressão (2026-09-14): a foto do celular entrava no PDF na resolução original, e
    // um boleto fotografado virava dezenas de MB. Agora ela cabe na folha A4 a 200 DPI, em JPEG.
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    public void TryAppend_WithAPhoneSizedPhoto_ShouldFitItOnAnA4SheetAtPrintResolution(SKEncodedImageFormat format)
    {
        var photo = NoisyPhoto(3000, 2250, format);
        using var composition = Composer.Begin();

        Assert.True(composition.TryAppend(photo, "application/octet-stream"));

        var pdf = composition.Build();
        using var document = PdfDocument.Open(pdf);
        var image = Assert.Single(document.GetPage(1).GetImages());

        // A4 deitada, área útil de 746 × 499 pt: a 200 DPI isso são no máximo 2072 × 1386 px.
        Assert.True(image.WidthInSamples <= 2072, $"largura {image.WidthInSamples}");
        Assert.True(image.HeightInSamples <= 1386, $"altura {image.HeightInSamples}");
        Assert.True(pdf.Length < photo.Length / 2 || pdf.Length < 1_000_000, $"PDF com {pdf.Length} bytes");
    }

    // Foto deitada vai em A4 deitada; em pé, em A4 em pé — a folha acompanha a imagem.
    [Theory]
    [InlineData(1600, 900, 842, 595)]
    [InlineData(900, 1600, 595, 842)]
    public void TryAppend_WithAnImage_ShouldOrientTheSheetLikeTheImage(
        int pixelWidth, int pixelHeight, double pageWidth, double pageHeight)
    {
        using var composition = Composer.Begin();

        composition.TryAppend(SolidImage(pixelWidth, pixelHeight, SKEncodedImageFormat.Jpeg), "image/jpeg");

        using var document = PdfDocument.Open(composition.Build());
        var page = document.GetPage(1);
        Assert.Equal(pageWidth, page.Width, precision: 0);
        Assert.Equal(pageHeight, page.Height, precision: 0);
    }

    // O celular grava os pixels deitados e diz no EXIF que a foto é em pé: sem aplicar a
    // rotação, o boleto sairia de lado no PDF.
    [Fact]
    public void TryAppend_WithAnExifRotatedPhoto_ShouldApplyTheRotation()
    {
        var sensorLandscape = WithExifOrientation(SolidImage(1600, 900, SKEncodedImageFormat.Jpeg), orientation: 6);
        using var composition = Composer.Begin();

        composition.TryAppend(sensorLandscape, "image/jpeg");

        using var document = PdfDocument.Open(composition.Build());
        var page = document.GetPage(1);
        var image = Assert.Single(page.GetImages());
        Assert.True(page.Height > page.Width);
        Assert.True(image.HeightInSamples > image.WidthInSamples);
    }

    // Imagem pequena não é ampliada: ampliar só incharia o arquivo sem ganhar nitidez.
    [Fact]
    public void Fit_WithASmallImage_ShouldKeepItsSize()
    {
        var fitted = A4Image.Fit(SolidImage(120, 80, SKEncodedImageFormat.Png));

        Assert.NotNull(fitted);
        Assert.Equal(120, fitted!.PixelWidth);
        Assert.Equal(80, fitted.PixelHeight);
    }

    // PNG transparente vai para JPEG sobre fundo branco, não preto.
    [Fact]
    public void Fit_WithATransparentPng_ShouldPaintAWhiteBackground()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(50, 50, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);
        using var png = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);

        var fitted = A4Image.Fit(png.ToArray());

        using var decoded = SKBitmap.Decode(fitted!.Jpeg);
        var pixel = decoded.GetPixel(25, 25);
        Assert.True(pixel.Red > 240 && pixel.Green > 240 && pixel.Blue > 240, pixel.ToString());
    }

    // Documento que não abre devolve false e não deixa página nenhuma para trás — quem chama
    // põe o aviso no lugar.
    [Fact]
    public void TryAppend_WithUnreadableBytes_ShouldReturnFalseAndAddNothing()
    {
        using var composition = Composer.Begin();

        Assert.False(composition.TryAppend("%PDF-1.4 corrompido"u8.ToArray(), "application/pdf"));
        Assert.False(composition.TryAppend("<html></html>"u8.ToArray(), "text/html"));
        Assert.Equal(0, composition.PageCount);
    }

    // A página de aviso escreve acentuação do português e quebra o payload Pix, que não tem
    // espaço nenhum, em várias linhas sem perder caractere.
    [Fact]
    public void AppendNotice_WithAccentsAndAnUnbrokenPixPayload_ShouldWriteEverything()
    {
        var payload = string.Concat(Enumerable.Repeat("00020126580014br.gov.bcb.pix0136", 8));
        using var composition = Composer.Begin();

        composition.AppendNotice(new PdfNotice(
            "Boleto sem documento",
            "Não há documento guardado. Código obtido por inserção manual.",
            [new PdfNoticeField("Pix copia e cola", payload)]));

        using var document = PdfDocument.Open(composition.Build());
        var text = string.Concat(document.GetPages().Select(p => p.Text));

        Assert.Contains("Não há documento guardado", text, StringComparison.Ordinal);
        Assert.Contains(payload, text.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    // Aviso maior que uma página continua na seguinte em vez de escrever fora da folha.
    [Fact]
    public void AppendNotice_WhenTheFieldsDoNotFit_ShouldContinueOnANewPage()
    {
        using var composition = Composer.Begin();
        var fields = Enumerable.Range(1, 40).Select(i => new PdfNoticeField($"Campo {i}", $"Valor {i}")).ToList();

        composition.AppendNotice(new PdfNotice("Aviso", "Muitos campos.", fields));

        Assert.True(composition.PageCount > 1);
        using var document = PdfDocument.Open(composition.Build());
        Assert.Contains("Valor 40", document.GetPage(document.NumberOfPages).Text, StringComparison.Ordinal);
    }

    // Composição sem página não vira PDF vazio em silêncio.
    [Fact]
    public void Build_WithoutPages_ShouldThrow()
    {
        using var composition = Composer.Begin();

        Assert.Throws<InvalidOperationException>(() => composition.Build());
    }

    internal static byte[] PdfWithPages(string label, int pages)
    {
        using var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (var i = 1; i <= pages; i++)
            builder.AddPage(595, 842).AddText($"{label}-{i}", 12, new PdfPoint(40, 780), font);

        return builder.Build();
    }

    private static byte[] SolidImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.DarkOliveGreen);
        using var data = SKImage.FromBitmap(bitmap).Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>Uma "foto": gradiente com ruído, que comprime como imagem de câmera e não como cor lisa.</summary>
    private static byte[] NoisyPhoto(int width, int height, SKEncodedImageFormat format)
    {
        var random = new Random(42);
        var pixels = new SKColor[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var noise = random.Next(-16, 16);
                pixels[(y * width) + x] = new SKColor(
                    (byte)Math.Clamp((x * 255 / width) + noise, 0, 255),
                    (byte)Math.Clamp((y * 255 / height) + noise, 0, 255),
                    (byte)Math.Clamp(128 + noise, 0, 255));
            }
        }

        using var bitmap = new SKBitmap(width, height) { Pixels = pixels };
        using var data = SKImage.FromBitmap(bitmap).Encode(format, 92);
        return data.ToArray();
    }

    /// <summary>Insere um segmento APP1 com EXIF de uma tag só — a orientação — logo após o SOI.</summary>
    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            (byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x01,
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, (byte)(orientation >> 8), (byte)orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        return [.. jpeg[..2], .. exif, .. jpeg[2..]];
    }

    internal static byte[] ImageBytes(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(120, 80);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(SKColors.SteelBlue);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }
}
