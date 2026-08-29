namespace BillPayment.Infra.Extraction;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using ZXing;
using ZXing.SkiaSharp;

/// <summary>
/// Degrau 2b da cascata: lê QR das imagens do documento e valida o CRC antes de aceitar.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é opcional.</strong> Medido três vezes — na sonda de produção (2026-08-06) e no
/// corpus completo (2026-08-11, <strong>zero</strong> BR Code no texto em 41 documentos) — o QR
/// existe só como imagem. Sem este degrau, o trilho que o ADR-010 elege como preferencial só
/// funcionaria pedindo ao usuário escanear com o celular e colar o "Copia e Cola".
/// </para>
/// <para>
/// <strong>Lê as imagens embutidas, não rasteriza a página.</strong> Boleto imprime o QR como
/// imagem embutida, e extraí-la custa uma fração de rasterizar a página inteira — que exigiria
/// um motor de renderização e um binário nativo a mais no contêiner. Se um emissor desenhar o QR
/// como vetor, ele cai para o extrator de visão da 2.4, e a métrica da cascata mostra isso.
/// </para>
/// <para>
/// <strong>A saída é candidato, não verdade</strong> — como a do modelo de visão. O funil é o
/// mesmo: CRC-16 aqui, consulta oficial depois (ADR-011).
/// </para>
/// </remarks>
internal static class QrCodeScanner
{
    /// <summary>
    /// Imagem menor que isto não carrega um QR legível — é ícone, logotipo ou separador. Pular
    /// evita gastar decodificação com o que enche uma página de boleto.
    /// </summary>
    private const int MIN_DIMENSION = 40;

    /// <summary>O código de barras FEBRABAN tem 44 posições, em cobrança e em arrecadação.</summary>
    private const int BARCODE_DIGITS = 44;

    /// <summary>
    /// Teto de pixels decodificados por imagem. Um PDF de 200 KB pode declarar uma imagem de
    /// 30.000×30.000 (3,6 GB decodificados) — bomba de descompressão por anexo hostil. 25 MP
    /// cobre qualquer boleto digitalizado com folga.
    /// </summary>
    private const long MAX_PIXELS = 25_000_000;

    // Um leitor por decodificação: o BarcodeReader do ZXing.Net não é thread-safe, e as faixas
    // rápida e de visão decodificam em paralelo.
    private static BarcodeReader CreateReader() => new()
    {
        AutoRotate = true,
        Options = new ZXing.Common.DecodingOptions
        {
            // QR (Pix) e ITF (o código de barras do boleto). A primeira versão só lia QR, com a
            // justificativa de que o código de barras "já vem do texto" — falsa exatamente
            // quando NÃO há texto, que é o caso do documento digitalizado. Medido no corpus:
            // há ITF legível em arquivos que estavam indo para o extrator de visão à toa.
            PossibleFormats = [BarcodeFormat.QR_CODE, BarcodeFormat.ITF],
            TryHarder = true,
            TryInverted = true,
        },
    };

    /// <summary>
    /// Uma página pode ter <strong>mais de um QR</strong> — logotipo com QR institucional, QR de
    /// outra finalidade. Todos viram candidato; só os que passam no CRC sobrevivem, e a consulta
    /// oficial desempata se sobrar mais de um.
    /// </summary>
    public static IReadOnlyList<PaymentInstrument> Scan(
        IEnumerable<IPdfImage> images,
        HashSet<string> seen,
        DateTime today,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var found = new List<PaymentInstrument>();

        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (image.WidthInSamples < MIN_DIMENSION || image.HeightInSamples < MIN_DIMENSION)
                continue;

            if ((long)image.WidthInSamples * image.HeightInSamples > MAX_PIXELS)
            {
                logger.LogWarning(
                    "Imagem de {Width}x{Height} ignorada pelo leitor de QR: acima do teto de pixels.",
                    image.WidthInSamples, image.HeightInSamples);
                continue;
            }

            foreach (var (format, text) in DecodeAll(image, logger))
            {
                var instrument = Build(format, text, today);

                if (instrument is not null && seen.Add(instrument.NaturalKey))
                    found.Add(instrument);
            }
        }

        return found;
    }

    /// <summary>
    /// Converte a imagem do PDF em bitmap, por dois caminhos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>TryGetPng</c> normaliza vários formatos internos do PDF, mas <strong>falha em
    /// <c>/DCTDecode</c></strong> — que é JPEG, e é o que as concessionárias usam. Medido no
    /// corpus real (2026-08-11): numa conta de luz, <strong>8 das 13 imagens</strong> eram
    /// DCTDecode e nenhuma era legível por esse caminho, incluindo as faixas que contêm o corpo
    /// da página.
    /// </para>
    /// <para>
    /// O segundo caminho resolve isso sem custo: para <c>/DCTDecode</c> os bytes brutos
    /// <em>já são</em> um JPEG, e o SkiaSharp o decodifica nativamente. Confiar só no
    /// <c>TryGetPng</c> descartaria documento legível como se precisasse de visão.
    /// </para>
    /// </remarks>
    private static SKBitmap? ToBitmap(IPdfImage image)
    {
        if (image.TryGetPng(out var png) && png is not null)
        {
            var fromPng = SKBitmap.Decode(png);
            if (fromPng is not null)
                return fromPng;
        }

        // JPEG, JPEG2000 e afins chegam prontos nos bytes brutos.
        var raw = image.RawMemory;
        return raw.IsEmpty ? null : SKBitmap.Decode(raw.Span);
    }

    /// <summary>
    /// Traduz o que foi decodificado no instrumento correspondente, ou nada.
    /// </summary>
    /// <remarks>
    /// Os dois caminhos passam pelo mesmo funil determinístico: CRC-16 no BR Code, dígitos
    /// verificadores no código de barras. <c>DomainException</c> aqui é fluxo normal — QR de
    /// rastreamento e barra de outra finalidade são o caso comum numa página de boleto.
    /// </remarks>
    private static PaymentInstrument? Build(BarcodeFormat format, string text, DateTime today)
    {
        try
        {
            if (format == BarcodeFormat.QR_CODE)
                return PaymentInstrument.FromPixQr(PixPayload.Parse(text));

            // O ITF impresso no boleto é o código de barras de 44 posições; a linha digitável
            // é reconstruída a partir dele, com todos os DVs conferidos.
            var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
            return digits.Length != BARCODE_DIGITS
                ? null
                : PaymentInstrument.FromBarcode(DigitableLine.FromBarcode(digits, today));
        }
        catch (DomainException)
        {
            return null;
        }
    }

    /// <summary>
    /// Decodifica <strong>todos</strong> os códigos de uma imagem, não só o primeiro.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o que faz o boleto de dois QR funcionar</strong>, e ele é comum: concessionária
    /// imprime um QR de nota fiscal e outro de Pix, muitas vezes dentro da mesma imagem embutida.
    /// Com <c>Decode</c> — que devolve um código só — o QR da nota é encontrado primeiro e o Pix
    /// nunca é visto. A falha seria silenciosa: o documento resolveria pelo código de barras e
    /// ninguém notaria que o trilho preferencial sumiu.
    /// </para>
    /// <para>
    /// Achado na conta de luz da EDP do corpus real (2026-08-11), e confirmado pelo usuário como
    /// padrão de vários emissores.
    /// </para>
    /// </remarks>
    private static List<(BarcodeFormat Format, string Text)> DecodeAll(IPdfImage image, ILogger logger)
    {
        try
        {
            using var bitmap = ToBitmap(image);
            if (bitmap is null)
                return [];

            var reader = CreateReader();
            var results = reader.DecodeMultiple(bitmap);
            if (results is not null && results.Length > 0)
            {
                return results
                    .Where(r => !string.IsNullOrWhiteSpace(r.Text))
                    .Select(r => (r.BarcodeFormat, r.Text))
                    .ToList();
            }

            // DecodeMultiple é mais exigente que Decode em imagem ruidosa: quando ele não acha
            // nada, ainda vale a tentativa simples antes de mandar o documento para a visão.
            var single = reader.Decode(bitmap);
            return single is null || string.IsNullOrWhiteSpace(single.Text)
                ? []
                : [(single.BarcodeFormat, single.Text)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Imagem corrompida ou em formato que o decodificador não abre não invalida o
            // documento: as outras imagens da página continuam sendo tentadas.
            logger.LogDebug(ex, "Não foi possível decodificar uma imagem do PDF.");
            return [];
        }
    }
}
