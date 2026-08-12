namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O documento que a escada de resolução de link conseguiu trazer, e de onde ele veio.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A URL viaja junto porque ela é a evidência.</strong> Um boleto que chegou por link não
/// tem anexo para reapresentar: se alguém perguntar depois "de onde saiu este documento", a única
/// resposta é o endereço que foi buscado. É o que o <c>CaptureItem.SourceUrl</c> guarda.
/// </para>
/// <para>
/// <strong>Não é boleto — é um arquivo.</strong> O que veio pela rede passa pela mesma cascata
/// determinística de um anexo: DV da linha digitável, CRC do BR Code, senha derivada. Nada aqui
/// dispensa nenhum degrau; a origem remota só muda como os bytes chegaram.
/// </para>
/// </remarks>
public sealed class ResolvedDocument : ValueObject
{
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Tipo declarado pelo servidor de onde o documento veio, já sem parâmetros.</summary>
    public string? MediaType { get; }

    /// <summary>O endereço que produziu estes bytes — sigiloso como o próprio documento.</summary>
    public string SourceUrl { get; }

    private ResolvedDocument(ReadOnlyMemory<byte> content, string? mediaType, string sourceUrl)
    {
        Content = content;
        MediaType = mediaType;
        SourceUrl = sourceUrl;
    }

    public static ResolvedDocument From(ReadOnlyMemory<byte> content, string? mediaType, string sourceUrl)
    {
        if (content.IsEmpty)
            throw ExtractionErrors.PayloadRequired();

        var url = sourceUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw ExtractionErrors.SourceUrlRequired();

        if (url.Length > DocumentLink.URL_MAX_LENGTH)
            url = url[..DocumentLink.URL_MAX_LENGTH];

        var normalized = mediaType?.Trim().ToLowerInvariant();
        var separator = normalized?.IndexOf(';', StringComparison.Ordinal) ?? -1;
        if (separator > 0)
            normalized = normalized![..separator].Trim();

        return new ResolvedDocument(content, string.IsNullOrEmpty(normalized) ? null : normalized, url);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SourceUrl;
        yield return MediaType;
        yield return Content.Length;
    }
}
