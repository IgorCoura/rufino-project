namespace BillPayment.Infra.Extraction;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Content;
using ZXing.Common;
using ZXing.QrCode.Internal;

/// <summary>
/// Degrau 2c da cascata: lê o QR desenhado como TEXTO — uma grade de caracteres <c>0</c>/<c>1</c>
/// numa fonte cujo glifo é um quadradinho — e valida o CRC antes de aceitar.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe por causa da fatura da Vivo (2026-09-14).</strong> O PDF traz o QR Pix numa
/// fonte chamada <c>VivoQRCode</c>: 4.225 caracteres, uma grade de 65×65 com a margem branca, em
/// que <c>1</c> é módulo escuro e <c>0</c> é claro. Não há imagem nenhuma, então o
/// <see cref="QrCodeScanner"/> — que só lê imagens embutidas — não via QR, e o documento resolvia
/// só pelo código de barras: o trilho preferencial (ADR-010) e o check <c>PixBarcodeConsistency</c>
/// sumiam em silêncio. A visão também não salvava: com o código de barras resolvido, os candidatos
/// Pix do modelo não são usados, e modelo de linguagem não decodifica QR com confiabilidade.
/// </para>
/// <para>
/// <strong>Detecta pela GEOMETRIA, nunca pelo nome da fonte.</strong> O nome é do emissor e muda com
/// ele; a forma não: glifos <c>0</c>/<c>1</c> da mesma fonte e tamanho, alinhados em N linhas de N
/// colunas com passo constante. Texto comum não forma quadrado perfeito de centenas de caracteres.
/// </para>
/// <para>
/// <strong>A saída é candidato, não verdade</strong> — o mesmo funil do QR em imagem: CRC-16 aqui,
/// consulta oficial depois (ADR-011).
/// </para>
/// </remarks>
internal static class GlyphQrScanner
{
    /// <summary>O que a varredura achou numa página.</summary>
    /// <param name="GridGlyphs">
    /// Os glifos que formam alguma grade — decodificada ou não. <strong>Saem do texto que vai para
    /// as varreduras de dígitos</strong>: uma grade de 65×65 são 4.225 dígitos seguidos, e o varredor
    /// de linha digitável gera janelas de 47/48 posições que passam nos DVs por acaso. Medido no teste
    /// da grade invertida (2026-09-14): quatro "códigos de barras" fabricados de uma grade só.
    /// </param>
    public sealed record Result(IReadOnlyList<PaymentInstrument> Instruments, IReadOnlySet<Letter> GridGlyphs);

    /// <summary>O menor QR (versão 1) tem 21 módulos por lado.</summary>
    private const int MIN_QR_SIDE = 21;

    /// <summary>O maior QR (versão 40) tem 177 módulos; a margem branca pode somar até 8 por eixo.</summary>
    private const int MAX_GRID_SIDE = 177 + 16;

    /// <summary>Folga no alinhamento — as coordenadas saem do PDF em ponto flutuante.</summary>
    private const double POSITION_TOLERANCE = 0.25;

    public static Result Scan(
        IEnumerable<Letter> letters,
        HashSet<string> seen,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var found = new List<PaymentInstrument>();
        var gridGlyphs = new HashSet<Letter>(ReferenceEqualityComparer.Instance);

        // Uma fonte por grupo: o QR usa uma fonte só, e misturar os dígitos do texto comum com os
        // módulos faria a grade nunca fechar.
        var groups = letters
            .Where(l => l.Value is "0" or "1")
            .GroupBy(l => (l.FontName, Size: Math.Round(l.FontSize, 2)));

        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var glyphs = group.ToList();
            if (glyphs.Count < MIN_QR_SIDE * MIN_QR_SIDE || glyphs.Count > MAX_GRID_SIDE * MAX_GRID_SIDE)
                continue;

            var grid = ToGrid(glyphs);
            if (grid is null)
                continue;

            gridGlyphs.UnionWith(glyphs);

            foreach (var text in Decode(grid, logger))
            {
                var instrument = Build(text);

                if (instrument is not null && seen.Add(instrument.NaturalKey))
                    found.Add(instrument);
            }
        }

        return new Result(found, gridGlyphs);
    }

    /// <summary>
    /// Monta a grade de módulos — <c>true</c> é o caractere <c>1</c> — ou nada, quando os glifos não
    /// formam um quadrado com passo constante.
    /// </summary>
    private static bool[,]? ToGrid(List<Letter> glyphs)
    {
        // De cima para baixo (Y do PDF cresce para cima), da esquerda para a direita.
        var rows = glyphs
            .GroupBy(l => Math.Round(l.StartBaseLine.Y / POSITION_TOLERANCE))
            .OrderByDescending(g => g.Key)
            .Select(g => g.OrderBy(l => l.StartBaseLine.X).ToList())
            .ToList();

        var side = rows.Count;
        if (side < MIN_QR_SIDE || side > MAX_GRID_SIDE || rows.Any(r => r.Count != side))
            return null;

        if (!HasConstantStep(rows.Select(r => r[0].StartBaseLine.Y).ToList())
            || rows.Any(r => !HasConstantStep(r.Select(l => l.StartBaseLine.X).ToList())))
        {
            return null;
        }

        var grid = new bool[side, side];
        for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
                grid[y, x] = rows[y][x].Value == "1";

        return grid;
    }

    private static bool HasConstantStep(List<double> positions)
    {
        var step = Math.Abs(positions[1] - positions[0]);
        if (step < POSITION_TOLERANCE)
            return false;

        for (var i = 2; i < positions.Count; i++)
        {
            if (Math.Abs(Math.Abs(positions[i] - positions[i - 1]) - step) > POSITION_TOLERANCE)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Decodifica a grade nas duas leituras — <c>1</c> escuro e <c>1</c> claro —, cortando a margem.
    /// </summary>
    /// <remarks>
    /// A fonte decide qual caractere pinta o módulo, e a da Vivo pinta o <c>1</c>. Tentar o inverso
    /// custa uma decodificação a mais e cobre o emissor que escolher o contrário.
    /// </remarks>
    private static IEnumerable<string> Decode(bool[,] grid, ILogger logger)
    {
        foreach (var dark in new[] { true, false })
        {
            var matrix = Crop(grid, dark);
            if (matrix is null)
                continue;

            string? text = null;
            try
            {
                text = new Decoder().decode(matrix, null)?.Text;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Grade de glifos não decodificou como QR.");
            }

            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    /// <summary>A caixa que contém os módulos escuros, se ela tiver o tamanho de um QR válido.</summary>
    private static BitMatrix? Crop(bool[,] grid, bool dark)
    {
        var side = grid.GetLength(0);
        int top = side, left = side, bottom = -1, right = -1;

        for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
            {
                if (grid[y, x] != dark)
                    continue;

                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }

        var height = bottom - top + 1;
        var width = right - left + 1;

        // Lado de QR válido: 21 + 4k. Qualquer outra caixa não é QR, e o decodificador nem precisa
        // ser incomodado.
        if (bottom < 0 || width != height || width < MIN_QR_SIDE || (width - MIN_QR_SIDE) % 4 != 0)
            return null;

        var matrix = new BitMatrix(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (grid[top + y, left + x] == dark)
                    matrix[x, y] = true;

        return matrix;
    }

    /// <summary>O mesmo funil do QR em imagem: só vira instrumento o que passa no CRC do BR Code.</summary>
    private static PaymentInstrument? Build(string text)
    {
        try
        {
            return PaymentInstrument.FromPixQr(PixPayload.Parse(text));
        }
        catch (DomainException)
        {
            // QR que não é Pix (nota fiscal, rastreamento) é o caso comum, não erro.
            return null;
        }
    }
}
