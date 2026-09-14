namespace BillPayment.Infra.Documents;

using System.Reflection;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

/// <summary>
/// Monta PDF com o PdfPig: copia páginas de outros PDFs, põe imagem numa página A4 e escreve a
/// página de aviso.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A fonte é embutida, e não uma das catorze padrão.</strong> As fontes padrão do PdfPig
/// não têm <c>ç</c> nem <c>ã</c> — escrever "Não há documento" nelas lança. A Roboto (Apache 2.0)
/// vai como recurso do assembly e só os glifos usados entram no arquivo.
/// </para>
/// </remarks>
internal sealed class PdfComposer(ILogger<PdfComposer> logger) : IPdfComposer
{
    private static readonly Lazy<byte[]> RegularFont = new(() => LoadFont("Roboto-Regular.ttf"));
    private static readonly Lazy<byte[]> BoldFont = new(() => LoadFont("Roboto-Bold.ttf"));

    public IPdfComposition Begin() => new Composition(logger);

    private static byte[] LoadFont(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(fileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private sealed class Composition(ILogger logger) : IPdfComposition
    {
        private const double PAGE_WIDTH = A4Image.A4_SHORT_SIDE;
        private const double PAGE_HEIGHT = A4Image.A4_LONG_SIDE;
        private const double MARGIN = A4Image.MARGIN;

        private readonly PdfDocumentBuilder _builder = new();

        // O PdfPig lê as páginas copiadas no Build, então as fontes delas ficam abertas até lá.
        private readonly List<PdfDocument> _sources = [];

        private PdfDocumentBuilder.AddedFont? _regular;
        private PdfDocumentBuilder.AddedFont? _bold;

        public int PageCount { get; private set; }

        public bool TryAppend(ReadOnlyMemory<byte> content, string contentType, int? maxPages = null)
        {
            if (content.IsEmpty)
                return false;

            var measured = DocumentMagic.MediaTypeOf(content.Span) ?? contentType;

            try
            {
                return measured switch
                {
                    "application/pdf" => AppendPdf(content, maxPages),
                    "image/png" or "image/jpeg" or "image/webp" => AppendImage(content),
                    _ => false,
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Documento de {ContentType} não pôde entrar na composição.", measured);
                return false;
            }
        }

        public void AppendNotice(PdfNotice notice)
        {
            ArgumentNullException.ThrowIfNull(notice);

            _regular ??= _builder.AddTrueTypeFont(RegularFont.Value);
            _bold ??= _builder.AddTrueTypeFont(BoldFont.Value);

            var writer = new NoticeWriter(this, _regular, _bold);
            writer.Write(notice);
        }

        public byte[] Build()
        {
            if (PageCount == 0)
                throw new InvalidOperationException("Composição sem páginas.");

            return _builder.Build();
        }

        public void Dispose()
        {
            foreach (var source in _sources)
                source.Dispose();

            _builder.Dispose();
        }

        private bool AppendPdf(ReadOnlyMemory<byte> content, int? maxPages)
        {
            var document = PdfDocument.Open(content.ToArray());
            var total = document.NumberOfPages;

            if (total == 0)
            {
                document.Dispose();
                return false;
            }

            _sources.Add(document);

            var take = maxPages is > 0 ? Math.Min(maxPages.Value, total) : total;
            for (var page = 1; page <= take; page++)
                _builder.AddPage(document, page);

            PageCount += take;
            return true;
        }

        /// <summary>
        /// Toda imagem — PNG, JPEG ou WEBP — é normalizada para uma folha A4 antes de entrar: a
        /// foto do celular na resolução original fazia o PDF passar de dezenas de MB.
        /// </summary>
        private bool AppendImage(ReadOnlyMemory<byte> content)
        {
            var fitted = A4Image.Fit(content.ToArray());
            if (fitted is null)
                return false;

            var (pageWidth, pageHeight) = fitted.Landscape
                ? (A4Image.A4_LONG_SIDE, A4Image.A4_SHORT_SIDE)
                : (A4Image.A4_SHORT_SIDE, A4Image.A4_LONG_SIDE);

            var page = _builder.AddPage(pageWidth, pageHeight);
            page.AddJpeg(fitted.Jpeg, FitInsideMargins(fitted, pageWidth, pageHeight));

            PageCount++;
            return true;
        }

        /// <summary>Centraliza a imagem na área útil, preservando a proporção.</summary>
        private static PdfRectangle FitInsideMargins(A4ImagePage image, double pageWidth, double pageHeight)
        {
            var (areaWidth, areaHeight) = A4Image.PrintableArea(image.Landscape);
            var scale = Math.Min(areaWidth / image.PixelWidth, areaHeight / image.PixelHeight);

            var drawnWidth = image.PixelWidth * scale;
            var drawnHeight = image.PixelHeight * scale;
            var left = (pageWidth - drawnWidth) / 2;
            var top = pageHeight - A4Image.MARGIN;

            return new PdfRectangle(left, top - drawnHeight, left + drawnWidth, top);
        }

        /// <summary>Escreve o aviso de cima para baixo, abrindo página nova quando acaba o espaço.</summary>
        private sealed class NoticeWriter(
            Composition composition,
            PdfDocumentBuilder.AddedFont regular,
            PdfDocumentBuilder.AddedFont bold)
        {
            private const double TITLE_SIZE = 18;
            private const double BODY_SIZE = 11;
            private const double LABEL_SIZE = 9;
            private const double LINE_GAP = 1.45;

            private PdfPageBuilder _page = default!;
            private double _y;

            public void Write(PdfNotice notice)
            {
                NewPage();

                WriteWrapped(notice.Title, bold, TITLE_SIZE, (0, 0, 0));
                _y -= 8;
                WriteWrapped(notice.Message, regular, BODY_SIZE, (60, 60, 60));
                _y -= 14;

                foreach (var field in notice.Fields)
                {
                    WriteWrapped(field.Label.ToUpperInvariant(), bold, LABEL_SIZE, (110, 110, 110));
                    WriteWrapped(string.IsNullOrWhiteSpace(field.Value) ? "—" : field.Value, regular, BODY_SIZE, (0, 0, 0));
                    _y -= 10;
                }
            }

            private void NewPage()
            {
                _page = composition._builder.AddPage(PAGE_WIDTH, PAGE_HEIGHT);
                composition.PageCount++;
                _y = PAGE_HEIGHT - MARGIN;
            }

            private void WriteWrapped(
                string text,
                PdfDocumentBuilder.AddedFont font,
                double size,
                (byte R, byte G, byte B) color)
            {
                var lineHeight = size * LINE_GAP;

                foreach (var line in Wrap(text, font, size, PAGE_WIDTH - (2 * MARGIN)))
                {
                    if (_y - lineHeight < MARGIN)
                        NewPage();

                    _y -= lineHeight;
                    _page.SetTextAndFillColor(color.R, color.G, color.B);
                    _page.AddText(line, size, new PdfPoint(MARGIN, _y), font);
                }
            }

            /// <summary>
            /// Quebra por palavra; palavra maior que a linha — o payload do Pix, a linha digitável
            /// sem espaço — quebra por caractere.
            /// </summary>
            private IEnumerable<string> Wrap(string text, PdfDocumentBuilder.AddedFont font, double size, double maxWidth)
            {
                foreach (var paragraph in text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
                {
                    var current = string.Empty;

                    foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var candidate = current.Length == 0 ? word : current + " " + word;
                        if (Width(candidate, font, size) <= maxWidth)
                        {
                            current = candidate;
                            continue;
                        }

                        if (current.Length > 0)
                            yield return current;

                        current = word;
                        while (Width(current, font, size) > maxWidth)
                        {
                            var fit = LongestPrefixThatFits(current, font, size, maxWidth);
                            yield return current[..fit];
                            current = current[fit..];
                        }
                    }

                    yield return current;
                }
            }

            private int LongestPrefixThatFits(string word, PdfDocumentBuilder.AddedFont font, double size, double maxWidth)
            {
                var low = 1;
                var high = word.Length;

                while (low < high)
                {
                    var middle = (low + high + 1) / 2;
                    if (Width(word[..middle], font, size) <= maxWidth)
                        low = middle;
                    else
                        high = middle - 1;
                }

                return low;
            }

            private double Width(string text, PdfDocumentBuilder.AddedFont font, double size)
            {
                if (text.Length == 0)
                    return 0;

                var letters = _page.MeasureText(text, size, new PdfPoint(0, 0), font);
                return letters.Count == 0 ? 0 : letters[^1].EndBaseLine.X;
            }
        }
    }
}
