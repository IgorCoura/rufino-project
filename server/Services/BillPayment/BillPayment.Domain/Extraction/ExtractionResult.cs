namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

/// <summary>
/// O que a cascata de extração conseguiu tirar de um artefato.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Os instrumentos aqui já são válidos por construção.</strong> <c>DigitableLine</c> só
/// existe com DV conferido e <c>PixPayload</c> só existe com CRC conferido — não há como esta
/// lista carregar candidato duvidoso. É por isso que "lista vazia" pode significar, com
/// segurança, "não é boleto".
/// </para>
/// <para>
/// <strong>Nada aqui é verdade sobre o pagamento</strong>, só sobre o documento. Quem diz que o
/// título existe, de quem é e por quanto é a consulta oficial (ADR-011). A extração propõe.
/// </para>
/// </remarks>
public sealed class ExtractionResult : ValueObject
{
    public const int REASON_CODE_MAX_LENGTH = 100;
    public const int UNLOCKED_BY_MAX_LENGTH = 100;

    private readonly List<PaymentInstrument> _instruments;
    private readonly List<PartyCandidate> _parties;

    /// <summary>
    /// Os instrumentos encontrados. Mais de um é normal: documento híbrido traz código de barras
    /// e QR, e uma página pode ter QR de outra finalidade — a consulta oficial desempata.
    /// </summary>
    public IReadOnlyList<PaymentInstrument> Instruments => _instruments.AsReadOnly();

    /// <summary>
    /// Os documentos fiscais lidos do artefato — do pagador <em>e</em> do beneficiário, sem
    /// distinção garantida entre eles.
    /// </summary>
    /// <remarks>
    /// Alimenta a escada de roteamento, e é por isso que a lista vem crua: quem sabe qual desses
    /// números importa é o <c>BillRoutingService</c>, confrontando com o cadastro do tenant. A
    /// extração não tem como decidir sozinha, e fingir que tem é o que faria o CNPJ da
    /// concessionária ser lido como o do pagador.
    /// </remarks>
    public IReadOnlyList<PartyCandidate> Parties => _parties.AsReadOnly();

    /// <summary>Qual degrau da cascata resolveu. Nulo quando nada resolveu.</summary>
    public ExtractionMethod? Method { get; }

    /// <summary>
    /// <strong>Qual campo</strong> do perfil derivou a senha do PDF — jamais a senha (ADR-009).
    /// </summary>
    public string? UnlockedBy { get; }

    /// <summary>Por que não resolveu, em código estável. Nulo quando resolveu.</summary>
    public string? ReasonCode { get; }

    private ExtractionResult(
        List<PaymentInstrument> instruments,
        List<PartyCandidate> parties,
        ExtractionMethod? method,
        string? unlockedBy,
        string? reasonCode)
    {
        _instruments = instruments;
        _parties = parties;
        Method = method;
        UnlockedBy = unlockedBy;
        ReasonCode = reasonCode;
    }

    public bool Resolved => _instruments.Count > 0;

    public static ExtractionResult Found(
        IEnumerable<PaymentInstrument> instruments,
        ExtractionMethod method,
        string? unlockedBy = null,
        IEnumerable<PartyCandidate>? parties = null)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        ArgumentNullException.ThrowIfNull(method);

        var list = instruments.ToList();
        if (list.Count == 0)
            throw ExtractionErrors.InstrumentRequired();

        return new ExtractionResult(
            list,
            parties?.Distinct().ToList() ?? [],
            method,
            Clamp(unlockedBy, UNLOCKED_BY_MAX_LENGTH),
            reasonCode: null);
    }

    /// <summary>
    /// Nada de pagável no artefato. <strong>Não é erro</strong> — é o desfecho mais comum numa
    /// caixa de uso misto, e o que permite descartar sem encher fila.
    /// </summary>
    public static ExtractionResult NotFound(string reasonCode)
        => string.IsNullOrWhiteSpace(reasonCode)
            ? throw ExtractionErrors.ReasonCodeRequired()
            : new ExtractionResult(
                [], [], method: null, unlockedBy: null, Clamp(reasonCode, REASON_CODE_MAX_LENGTH));

    /// <summary>
    /// PDF cifrado que nenhum candidato de senha abriu. Distinto de <see cref="NotFound"/> de
    /// propósito: aqui <em>não se sabe</em> se há boleto dentro, e descartar jogaria fora um
    /// documento que talvez seja exatamente o que se procura.
    /// </summary>
    public static ExtractionResult Locked()
        => new([], [], method: null, unlockedBy: null, "pdf_locked");

    public bool IsLocked => string.Equals(ReasonCode, "pdf_locked", StringComparison.Ordinal);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Method;
        yield return UnlockedBy;
        yield return ReasonCode;

        foreach (var instrument in _instruments)
            yield return instrument;

        foreach (var party in _parties)
            yield return party;
    }

    private static string? Clamp(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
