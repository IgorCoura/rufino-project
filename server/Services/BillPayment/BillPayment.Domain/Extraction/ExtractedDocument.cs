namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que o extrator de visão <em>propôs</em> a partir de um documento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É um saco de candidatos, não uma resposta.</strong> A diferença em relação ao
/// <see cref="ExtractionResult"/> é o ponto inteiro do ADR-011: lá os instrumentos já são válidos
/// por construção (<c>DigitableLine</c> só existe com DV conferido); aqui são strings que o
/// modelo achou que viu. Nada daqui vira pagamento sem atravessar DV, CRC, filtro de
/// plausibilidade e consulta oficial.
/// </para>
/// <para>
/// <strong><see cref="Amount"/> e <see cref="DueDate"/> existem só para conferência cruzada.</strong>
/// Quem diz quanto se paga e para quem é a consulta oficial no provedor. Um handler que use estes
/// campos para decidir valor, vencimento ou beneficiário é violação do ADR-011 — eles estão aqui
/// para <em>discordar</em> do trilho oficial e levantar suspeita, não para substituí-lo.
/// </para>
/// <para>
/// <strong>Nenhum termo de IA cruza esta fronteira.</strong> Sem <c>model</c>, sem <c>prompt</c>,
/// sem <c>token</c>, sem <c>temperature</c>. Se um deles aparecer aqui, o acoplamento vazou e o
/// ADR-013 deixou de valer.
/// </para>
/// </remarks>
public sealed class ExtractedDocument : ValueObject
{
    /// <summary>
    /// Teto de candidatos por lista. Existe porque a resposta vem de fora: um modelo que devolva
    /// mil linhas faria o funil determinístico gastar mil validações num documento só.
    /// </summary>
    public const int MAX_CANDIDATES = 20;

    public const int TEXT_MAX_LENGTH = 200;

    private readonly List<string> _digitableLineCandidates;
    private readonly List<string> _pixPayloadCandidates;

    /// <summary>Linhas digitáveis que o modelo viu. Cada uma ainda precisa passar pelo DV.</summary>
    public IReadOnlyList<string> DigitableLineCandidates => _digitableLineCandidates.AsReadOnly();

    /// <summary>BR Codes vistos, do QR ou do "copia e cola". Cada um ainda precisa passar pelo CRC.</summary>
    public IReadOnlyList<string> PixPayloadCandidates => _pixPayloadCandidates.AsReadOnly();

    /// <summary>Opinião sobre o tipo do documento. Métrica e evidência, nunca decisão.</summary>
    public DocumentKind? Kind { get; private init; }

    public string? PayerName { get; private init; }

    /// <summary>Documento do pagador <strong>como texto</strong>: só vira <c>TaxId</c> depois do DV.</summary>
    public string? PayerTaxId { get; private init; }

    public string? PayeeName { get; private init; }

    public string? PayeeTaxId { get; private init; }

    /// <summary>
    /// Identificador da conta no fornecedor — instalação da concessionária, matrícula, unidade.
    /// Alimenta o roteamento (2.6) e a expectativa de boleto (2.7).
    /// </summary>
    public string? AccountReference { get; private init; }

    /// <summary>Valor lido no documento, <strong>só para conferir</strong> contra a consulta oficial.</summary>
    public decimal? Amount { get; private init; }

    /// <summary>Vencimento lido no documento, <strong>só para conferir</strong>.</summary>
    public DateOnly? DueDate { get; private init; }

    /// <summary>
    /// A competência a que a conta se refere, como o documento (ou o corpo do e-mail) a declara —
    /// "07/2026", "julho/2026". Texto cru: quem normaliza é o <c>DocumentReading</c>.
    /// </summary>
    public string? BillingPeriod { get; private init; }

    /// <summary>Descrição breve do que a conta trata, para o resumo da tela.</summary>
    public string? Description { get; private init; }

    /// <summary>Observação curta do extrator, para a evidência do check.</summary>
    public string? Notes { get; private init; }

    private ExtractedDocument(List<string> digitableLines, List<string> pixPayloads)
    {
        _digitableLineCandidates = digitableLines;
        _pixPayloadCandidates = pixPayloads;
    }

    /// <summary>Nada proposto — o modelo rodou e não viu instrumento nenhum.</summary>
    public static ExtractedDocument Empty { get; } = new([], []);

    public bool HasCandidates => _digitableLineCandidates.Count > 0 || _pixPayloadCandidates.Count > 0;

    /// <summary>
    /// Monta o resultado do extrator, aparando o que vier fora do contrato.
    /// </summary>
    /// <remarks>
    /// <strong>Apara em vez de recusar, de propósito.</strong> A resposta vem de um sistema que
    /// não controlamos: recusar o conjunto inteiro porque um campo veio comprido descartaria os
    /// candidatos bons junto. O que não presta é cortado; o que sobra segue para o DV, que é
    /// quem tem autoridade para reprovar.
    /// </remarks>
    public static ExtractedDocument From(
        IEnumerable<string>? digitableLineCandidates = null,
        IEnumerable<string>? pixPayloadCandidates = null,
        DocumentKind? kind = null,
        string? payerName = null,
        string? payerTaxId = null,
        string? payeeName = null,
        string? payeeTaxId = null,
        string? accountReference = null,
        decimal? amount = null,
        DateOnly? dueDate = null,
        string? notes = null,
        string? billingPeriod = null,
        string? description = null)
        => new(Sanitize(digitableLineCandidates), Sanitize(pixPayloadCandidates))
        {
            Kind = kind,
            PayerName = Clamp(payerName),
            PayerTaxId = Clamp(payerTaxId),
            PayeeName = Clamp(payeeName),
            PayeeTaxId = Clamp(payeeTaxId),
            AccountReference = Clamp(accountReference),

            // Valor negativo não é boleto; zero também não. Vem de fora, então é aparado aqui em
            // vez de virar exceção — e de todo modo quem paga é a consulta oficial.
            Amount = amount is > 0 ? amount : null,
            DueDate = dueDate,
            BillingPeriod = Clamp(billingPeriod),
            Description = Clamp(description),
            Notes = Clamp(notes),
        };

    private static List<string> Sanitize(IEnumerable<string>? candidates)
        => candidates is null
            ? []
            : [.. candidates
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.Ordinal)
                .Take(MAX_CANDIDATES)];

    private static string? Clamp(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > TEXT_MAX_LENGTH ? trimmed[..TEXT_MAX_LENGTH] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return PayerName;
        yield return PayerTaxId;
        yield return PayeeName;
        yield return PayeeTaxId;
        yield return AccountReference;
        yield return Amount;
        yield return DueDate;
        yield return BillingPeriod;
        yield return Description;
        yield return Notes;

        foreach (var candidate in _digitableLineCandidates)
            yield return candidate;

        foreach (var candidate in _pixPayloadCandidates)
            yield return candidate;
    }
}
