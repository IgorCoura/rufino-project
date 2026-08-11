namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O documento que sai do perímetro para ser lido por um extrator externo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só o documento sai — nunca a caixa de e-mail nem o cadastro.</strong> É a página do
/// boleto e o tipo de mídia, mais nada. O que o sistema sabe sobre o tenant viaja separado, em
/// <see cref="ExtractionHints"/>, e por decisão consciente registrada no doc 10.
/// </para>
/// <para>
/// <strong>Aceita imagem, e isso não é detalhe.</strong> A cascata determinística só abre PDF, e
/// a medição de 2026-08-11 encontrou <strong>12 anexos recusados com <c>not_a_pdf</c></strong> —
/// baixados e nunca lidos. Se o extrator de visão também exigisse PDF, esses documentos
/// continuariam inalcançáveis.
/// </para>
/// </remarks>
public sealed class DocumentPayload : ValueObject
{
    /// <summary>
    /// Teto do que se manda para fora. O provedor aceita bem mais, mas anexo grande num
    /// documento de cobrança é sinal de outra coisa — e pagar leitura de vídeo por engano é o
    /// tipo de custo que ninguém percebe até a fatura.
    /// </summary>
    public const int MAX_BYTES = 20 * 1024 * 1024;

    public const string PDF = "application/pdf";

    private static readonly string[] SupportedMediaTypes = [PDF, "image/png", "image/jpeg", "image/webp"];

    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Tipo de mídia normalizado, já sabidamente suportado pelo extrator.</summary>
    public string MediaType { get; }

    public TenantId TenantId { get; }

    private DocumentPayload(ReadOnlyMemory<byte> content, string mediaType, TenantId tenantId)
    {
        Content = content;
        MediaType = mediaType;
        TenantId = tenantId;
    }

    public static DocumentPayload From(TenantId tenantId, ReadOnlyMemory<byte> content, string? contentType)
    {
        if (content.IsEmpty)
            throw ExtractionErrors.PayloadRequired();

        if (content.Length > MAX_BYTES)
            throw ExtractionErrors.PayloadTooLarge(MAX_BYTES);

        var normalized = Normalize(contentType);
        if (normalized is null)
            throw ExtractionErrors.UnsupportedMediaType(contentType ?? "(vazio)");

        return new DocumentPayload(content, normalized, tenantId);
    }

    /// <summary>Se este tipo de mídia pode ser lido — usado para decidir antes de gastar.</summary>
    public static bool IsSupported(string? contentType) => Normalize(contentType) is not null;

    /// <summary>
    /// <c>application/octet-stream</c> vira PDF: é como parte dos emissores rotula o anexo, e a
    /// allowlist do adapter de caixa já o aceita por isso. O extrator recusa depois se não for.
    /// </summary>
    private static string? Normalize(string? contentType)
    {
        var value = contentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
            return null;

        // "application/pdf; charset=binary" — o parâmetro não muda o tipo.
        var separator = value.IndexOf(';', StringComparison.Ordinal);
        if (separator > 0)
            value = value[..separator].Trim();

        if (string.Equals(value, "application/octet-stream", StringComparison.Ordinal))
            return PDF;

        if (string.Equals(value, "image/jpg", StringComparison.Ordinal))
            return "image/jpeg";

        return Array.Exists(SupportedMediaTypes, t => string.Equals(t, value, StringComparison.Ordinal))
            ? value
            : null;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MediaType;
        yield return TenantId;
        yield return Content.Length;
    }
}

/// <summary>
/// O que o sistema já sabe, e que melhora a leitura de campo cortado ou borrado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Isto sai do perímetro junto com o documento.</strong> São documentos do próprio
/// pagador e nomes de beneficiários que ele cadastrou — escolha consciente do doc 10, que deve
/// constar do aviso de privacidade. Nada de outro tenant entra aqui: os <c>Payee</c> e os
/// <c>TaxId</c> vêm do tenant dono do artefato, e o remetente é o do próprio e-mail.
/// </para>
/// <para>
/// Serve para reduzir alucinação: um modelo que já viu "estes são os CNPJs do pagador" tem menos
/// margem para inventar um. Continua sendo dica, não autoridade — o DV decide.
/// </para>
/// </remarks>
public sealed class ExtractionHints : ValueObject
{
    public const int MAX_ITEMS = 20;

    private readonly List<string> _payerTaxIds;
    private readonly List<string> _knownPayeeNames;

    /// <summary>Documentos do pagador, em dígitos.</summary>
    public IReadOnlyList<string> PayerTaxIds => _payerTaxIds.AsReadOnly();

    /// <summary>Razões sociais e apelidos de beneficiários que este tenant cadastrou.</summary>
    public IReadOnlyList<string> KnownPayeeNames => _knownPayeeNames.AsReadOnly();

    /// <summary>Remetente do e-mail de onde o artefato veio.</summary>
    public string? SenderAddress { get; private init; }

    private ExtractionHints(List<string> payerTaxIds, List<string> knownPayeeNames)
    {
        _payerTaxIds = payerTaxIds;
        _knownPayeeNames = knownPayeeNames;
    }

    public static ExtractionHints None { get; } = new([], []);

    public static ExtractionHints From(
        IEnumerable<string>? payerTaxIds = null,
        IEnumerable<string>? knownPayeeNames = null,
        string? senderAddress = null)
        => new(Take(payerTaxIds), Take(knownPayeeNames)) { SenderAddress = senderAddress?.Trim() };

    private static List<string> Take(IEnumerable<string>? values)
        => values is null
            ? []
            : [.. values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MAX_ITEMS)];

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SenderAddress;

        foreach (var taxId in _payerTaxIds)
            yield return taxId;

        foreach (var name in _knownPayeeNames)
            yield return name;
    }
}
