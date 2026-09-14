namespace BillPayment.Domain.Ports;

/// <summary>
/// Junta documentos num PDF só — o que a exportação de documentos de boletos entrega.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe como porta pelo mesmo motivo do <see cref="IBoletoDocumentParser"/></strong>: a
/// biblioteca de PDF é detalhe da Infra, e quem decide o que entra, em que ordem e com que aviso é
/// a Application. <c>Infra → Application</c> seria ciclo, então o contrato mora aqui.
/// </para>
/// <para>
/// <strong>Documento que não abre não lança</strong> — <see cref="IPdfComposition.TryAppend"/>
/// devolve <c>false</c>, e quem chama põe uma página de aviso no lugar. Um boleto sumir em silêncio
/// de dentro do arquivo é o pior desfecho possível numa exportação.
/// </para>
/// </remarks>
public interface IPdfComposer
{
    /// <summary>Abre uma composição vazia. Quem chama a descarta.</summary>
    IPdfComposition Begin();
}

/// <summary>Um PDF em montagem, página a página.</summary>
public interface IPdfComposition : IDisposable
{
    /// <summary>Quantas páginas já entraram.</summary>
    int PageCount { get; }

    /// <summary>
    /// Acrescenta um documento — PDF, PNG, JPEG ou WEBP. Imagem vira uma página.
    /// </summary>
    /// <param name="content">Os bytes, já sem senha quando o original era cifrado.</param>
    /// <param name="contentType">O tipo medido nos bytes.</param>
    /// <param name="maxPages">Quantas páginas do começo entram. Nulo = todas.</param>
    /// <returns><c>false</c> quando o documento não pôde ser lido; nada é acrescentado.</returns>
    bool TryAppend(ReadOnlyMemory<byte> content, string contentType, int? maxPages = null);

    /// <summary>Acrescenta uma página de aviso, que continua na página seguinte se não couber.</summary>
    void AppendNotice(PdfNotice notice);

    /// <summary>O PDF montado. Exige ao menos uma página.</summary>
    byte[] Build();
}

/// <summary>O conteúdo de uma página de aviso: título, explicação e campos rotulados.</summary>
public sealed record PdfNotice(string Title, string Message, IReadOnlyList<PdfNoticeField> Fields);

/// <summary>Um campo rotulado da página de aviso. Valor longo quebra em várias linhas.</summary>
public sealed record PdfNoticeField(string Label, string Value);
