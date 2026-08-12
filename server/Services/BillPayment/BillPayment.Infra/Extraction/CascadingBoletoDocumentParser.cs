namespace BillPayment.Infra.Extraction;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;

/// <summary>
/// Encaminha o artefato ao parser que sabe abri-lo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque a captura deixou de ser só de anexo.</strong> Desde a 2.5 um
/// <c>CaptureItem</c> pode ser o corpo da mensagem, e o parser de PDF recusava esse artefato com
/// <c>not_a_pdf</c> — o mesmo desfecho que, medido na 2.3, escondeu 12 boletos em imagem por
/// semanas. Rotear pelo tipo é o que impede o erro de se repetir por outro caminho.
/// </para>
/// <para>
/// <strong>O tipo declarado manda, os bytes desempatam.</strong> O <c>ContentType</c> é o que veio
/// do provedor na ingestão; quando ele não diz nada de útil, a assinatura do conteúdo decide. A
/// alternativa — deduzir do <c>ArtifactKey</c> — já foi tentada e rotulava todo anexo como PDF.
/// </para>
/// </remarks>
internal sealed class CascadingBoletoDocumentParser(
    PdfBoletoDocumentParser pdfParser,
    EmailBodyDocumentParser bodyParser) : IBoletoDocumentParser
{
    public Task<ExtractionResult> ParseAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var parser = IsTextual(contentType, content.Span) ? (IBoletoDocumentParser)bodyParser : pdfParser;

        return parser.ParseAsync(content, contentType, passwordCandidates, today, cancellationToken);
    }

    private static bool IsTextual(string? contentType, ReadOnlySpan<byte> content)
    {
        var declared = contentType?.Trim().ToLowerInvariant();

        var separator = declared?.IndexOf(';', StringComparison.Ordinal) ?? -1;
        if (separator > 0)
            declared = declared![..separator].Trim();

        if (declared is "text/html" or "text/plain")
            return true;

        // Sem tipo declarado o conteúdo decide. Só HTML é reconhecido pelos bytes: texto puro não
        // tem assinatura, e chutar "é texto" faria PDF corrompido cair no parser errado.
        return string.IsNullOrEmpty(declared) && HtmlText.LooksLikeHtml(content);
    }
}
