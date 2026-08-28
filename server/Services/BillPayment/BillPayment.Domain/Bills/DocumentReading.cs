namespace BillPayment.Domain.Bills;

using System.Globalization;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O retrato da leitura por IA do documento e do corpo do e-mail — irmão de
/// <c>LookupSnapshot</c>, e como ele um <strong>retrato</strong>, nunca a fonte de decisão.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nada daqui move dinheiro (ADR-011).</strong> Valor e beneficiário de pagamento vêm da
/// consulta oficial; este retrato existe para <em>enriquecer</em> (competência, descrição,
/// pagador, referência de conta) e para <em>contradizer</em> — o check que compara o documento
/// impresso com a consulta oficial lê daqui. A única exceção deliberada, decidida em 2026-08-27,
/// é o vencimento: quando nem a consulta oficial nem a linha digitável trazem data (QR estático),
/// a lida do documento preenche o vencimento consolidado.
/// </para>
/// <para>
/// <strong>Campos tipados e aparados, não JSON cru</strong> — guardrail 2 do doc 10: persiste-se
/// o resultado validado; reextração parte do original no storage. Os documentos fiscais só
/// entram com DV válido (<c>TaxId.TryParse</c>); número ilegível vira ausência.
/// </para>
/// </remarks>
public sealed class DocumentReading : ValueObject
{
    public const int TEXT_MAX_LENGTH = ExtractedDocument.TEXT_MAX_LENGTH;

    public string? PayerName { get; private init; }

    /// <summary>Documento do pagador lido, já provado pelo DV. Nulo quando ilegível.</summary>
    public TaxId? PayerTaxId { get; private init; }

    public string? PayeeName { get; private init; }

    /// <summary>Documento do beneficiário lido, já provado pelo DV. Nulo quando ilegível.</summary>
    public TaxId? PayeeTaxId { get; private init; }

    /// <summary>Instalação, matrícula, unidade ou contrato — alimenta roteamento e expectativa.</summary>
    public string? AccountReference { get; private init; }

    /// <summary>Valor impresso, <strong>só para conferência</strong> contra a consulta oficial.</summary>
    public decimal? Amount { get; private init; }

    /// <summary>Vencimento impresso. Última reserva do vencimento consolidado da <c>Bill</c>.</summary>
    public DateOnly? DueDate { get; private init; }

    /// <summary>A competência como o documento a declara — "07/2026", "julho/2026".</summary>
    public string? BillingPeriodText { get; private init; }

    /// <summary>A competência normalizada, quando o texto era parseável.</summary>
    public CompetencePeriod? Competence { get; private init; }

    /// <summary>Descrição breve do que a conta trata, para o resumo da tela.</summary>
    public string? Description { get; private init; }

    /// <summary>Observação do extrator, para evidência de check.</summary>
    public string? Notes { get; private init; }

    /// <summary>Quando esta leitura foi feita.</summary>
    public DateTimeOffset ReadAt { get; private init; }

    private DocumentReading() { }

    /// <summary>Se a leitura trouxe alguma coisa além do instante.</summary>
    public bool HasContent =>
        PayerName is not null || PayerTaxId is not null || PayeeName is not null
        || PayeeTaxId is not null || AccountReference is not null || Amount is not null
        || DueDate is not null || BillingPeriodText is not null || Description is not null;

    /// <summary>
    /// Compõe o retrato a partir do que o extrator propôs, validando o que é validável.
    /// </summary>
    public static DocumentReading FromExtraction(ExtractedDocument extracted, DateTimeOffset readAt)
    {
        ArgumentNullException.ThrowIfNull(extracted);

        var period = Clamp(extracted.BillingPeriod);

        return new DocumentReading
        {
            PayerName = Clamp(extracted.PayerName),
            PayerTaxId = TaxId.TryParse(extracted.PayerTaxId, out var payer) ? payer : null,
            PayeeName = Clamp(extracted.PayeeName),
            PayeeTaxId = TaxId.TryParse(extracted.PayeeTaxId, out var payee) ? payee : null,
            AccountReference = Clamp(extracted.AccountReference),
            Amount = extracted.Amount is > 0 ? extracted.Amount : null,
            DueDate = extracted.DueDate,
            BillingPeriodText = period,
            Competence = TryParseCompetence(period),
            Description = Clamp(extracted.Description),
            Notes = Clamp(extracted.Notes),
            ReadAt = readAt,
        };
    }

    /// <summary>Reidrata da persistência, sem revalidar — o que entrou já foi validado.</summary>
    public static DocumentReading Rehydrate(
        string? payerName,
        TaxId? payerTaxId,
        string? payeeName,
        TaxId? payeeTaxId,
        string? accountReference,
        decimal? amount,
        DateOnly? dueDate,
        string? billingPeriodText,
        CompetencePeriod? competence,
        string? description,
        string? notes,
        DateTimeOffset readAt)
        => new()
        {
            PayerName = payerName,
            PayerTaxId = payerTaxId,
            PayeeName = payeeName,
            PayeeTaxId = payeeTaxId,
            AccountReference = accountReference,
            Amount = amount,
            DueDate = dueDate,
            BillingPeriodText = billingPeriodText,
            Competence = competence,
            Description = description,
            Notes = notes,
            ReadAt = readAt,
        };

    private static readonly string[] MonthNames =
    [
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
    ];

    /// <summary>
    /// Normaliza a competência declarada — "07/2026", "7/2026", "2026-07", "julho/2026",
    /// "Julho de 2026". Texto fora desses formatos vira ausência, nunca chute.
    /// </summary>
    public static CompetencePeriod? TryParseCompetence(string? text)
    {
        var trimmed = text?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        // "março" sem acento também aparece — normaliza o caso conhecido antes de casar.
        trimmed = trimmed.Replace("marco", "março", StringComparison.Ordinal);

        var parts = trimmed.Split(['/', '-', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.Equals(p, "de", StringComparison.Ordinal))
            .ToArray();

        if (parts.Length != 2)
            return null;

        var (first, second) = (parts[0], parts[1]);

        // "2026-07" — ano na frente.
        if (first.Length == 4 && TryYear(first, out var yearFirst) && TryMonth(second, out var monthAfterYear))
            return Build(yearFirst, monthAfterYear);

        // "07/2026" e "julho/2026" — mês na frente.
        if (TryMonth(first, out var month) && TryYear(second, out var year))
            return Build(year, month);

        return null;
    }

    private static CompetencePeriod? Build(int year, int month)
        => year is >= CompetencePeriod.MIN_YEAR and <= CompetencePeriod.MAX_YEAR
            ? new CompetencePeriod(year, month)
            : null;

    private static bool TryYear(string value, out int year)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out year)
            && value.Length == 4;

    private static bool TryMonth(string value, out int month)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out month))
            return month is >= CompetencePeriod.MIN_MONTH and <= CompetencePeriod.MAX_MONTH;

        var index = Array.IndexOf(MonthNames, value);
        month = index + 1;
        return index >= 0;
    }

    private static string? Clamp(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > TEXT_MAX_LENGTH ? trimmed[..TEXT_MAX_LENGTH] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PayerName;
        yield return PayerTaxId;
        yield return PayeeName;
        yield return PayeeTaxId;
        yield return AccountReference;
        yield return Amount;
        yield return DueDate;
        yield return BillingPeriodText;
        yield return Competence;
        yield return Description;
        yield return Notes;
        yield return ReadAt;
    }
}
