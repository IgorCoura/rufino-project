namespace BillPayment.Infra.DocumentIntelligence;

using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

/// <summary>
/// Corta o PDF nas primeiras páginas antes de mandá-lo para fora.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Boleto está na primeira ou na segunda página — sempre.</strong> O que chega com
/// dezenas é outra coisa: relatório contábil, folha de pagamento, documentação mensal com o
/// boleto no meio de trinta anexos. Mandar o documento inteiro custa proporcional ao número de
/// páginas e não aumenta a chance de achar o código de barras.
/// </para>
/// <para>
/// <strong>Medido em 2026-08-11:</strong> sem o corte, chamadas de extração batiam no timeout de
/// 60 segundos e a vazão do processamento caiu de ~70 para ~8 artefatos por minuto — o teto de
/// páginas existia em <c>DocumentIntelligenceOptions</c> e não era aplicado em lugar nenhum.
/// </para>
/// <para>
/// <strong>Falhar aqui devolve o original.</strong> Se o arquivo não abre — não é PDF, está
/// cifrado, está corrompido —, o extrator recebe os bytes como vieram e decide por conta própria.
/// Um corte que não deu certo não pode virar documento perdido.
/// </para>
/// </remarks>
internal static class PdfPageTrimmer
{
    public static ReadOnlyMemory<byte> TakeFirstPages(
        ReadOnlyMemory<byte> content,
        int maxPages,
        ILogger logger)
    {
        if (maxPages <= 0)
            return content;

        try
        {
            using var document = PdfDocument.Open(content.ToArray());

            if (document.NumberOfPages <= maxPages)
                return content;

            var builder = new PdfDocumentBuilder();
            for (var page = 1; page <= maxPages; page++)
                builder.AddPage(document, page);

            var trimmed = builder.Build();

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Documento cortado de {Total} para {Kept} páginas antes da extração.",
                    document.NumberOfPages,
                    maxPages);
            }

            return trimmed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Não abriu: manda como veio. O extrator recusa se não souber ler, e isso é melhor
            // do que descartar um documento por causa de uma otimização.
            logger.LogDebug(ex, "Não foi possível cortar as páginas do documento; segue inteiro.");
            return content;
        }
    }
}
