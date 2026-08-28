namespace BillPayment.Domain.Extraction;

using BillPayment.Domain.SeedWork;

/// <summary>
/// O que voltou de uma tentativa de leitura por IA: o documento lido, ou o motivo de não haver um.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Falha de extração é modelada, não lançada</strong> — mesma doutrina do
/// <c>LookupResult</c> e do <c>MailboxResult</c>. Quem chama está num worker de fila, e uma
/// exceção ali derrubaria o processamento de um artefato por causa de um provedor instável.
/// </para>
/// <para>
/// <strong>Este VO não é persistido.</strong> O que fica gravado é o <c>DocumentReading</c> no
/// <c>Bill</c>; a tentativa é o veículo entre o adapter e quem decide o que fazer com ela.
/// </para>
/// </remarks>
public sealed class ExtractionAttempt : ValueObject
{
    public const int REASON_CODE_MAX_LENGTH = 100;

    public ExtractionStatus Status { get; }

    /// <summary>
    /// O que foi lido. <strong>Nunca nulo</strong> — vem vazio quando não houve leitura, para que
    /// o chamador que só quer os candidatos não precise conferir o status antes de tocar nele.
    /// </summary>
    public ExtractedDocument Document { get; }

    /// <summary>
    /// Código estável do motivo (<c>provider_unavailable</c>, <c>provider_rejected</c>,
    /// <c>budget_exhausted</c>). É por ele que se agrupa falha no relatório — a mensagem muda de
    /// redação, o código não. Nulo quando houve leitura.
    /// </summary>
    public string? ReasonCode { get; }

    private ExtractionAttempt(ExtractionStatus status, ExtractedDocument document, string? reasonCode)
    {
        Status = status;
        Document = document;
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode)
            ? null
            : reasonCode.Trim()[..Math.Min(reasonCode.Trim().Length, REASON_CODE_MAX_LENGTH)];
    }

    /// <summary>O modelo respondeu. Sem candidatos, o desfecho é <c>Empty</c>, não falha.</summary>
    public static ExtractionAttempt Answered(ExtractedDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.HasCandidates || HasReading(document)
            ? new ExtractionAttempt(ExtractionStatus.Resolved, document, reasonCode: null)
            : new ExtractionAttempt(ExtractionStatus.Empty, document, "nothing_extracted");
    }

    /// <summary>O provedor não respondeu. Nada foi aprendido sobre o documento.</summary>
    public static ExtractionAttempt Unavailable(string reasonCode)
        => new(ExtractionStatus.Unavailable, ExtractedDocument.Empty, reasonCode);

    /// <summary>O provedor recusou a requisição. Repetir produz a mesma recusa.</summary>
    public static ExtractionAttempt Rejected(string reasonCode)
        => new(ExtractionStatus.Rejected, ExtractedDocument.Empty, reasonCode);

    /// <summary>A cota do dia acabou, ou o intervalo mínimo ainda não passou.</summary>
    public static ExtractionAttempt BudgetExhausted()
        => new(ExtractionStatus.BudgetExhausted, ExtractedDocument.Empty, "budget_exhausted");

    /// <summary>Houve leitura utilizável.</summary>
    public bool IsResolved => Status.HasContent;

    /// <summary>Vale a pena tentar de novo. Só indisponibilidade e cota são retentáveis.</summary>
    public bool IsRetryable => Status.IsRetryable;

    /// <summary>
    /// O modelo respondeu — com ou sem candidatos.
    /// </summary>
    /// <remarks>
    /// É o que separa "o documento não é boleto" (desfecho sobre o artefato, que pode mandá-lo
    /// para a quarentena) de "o provedor falhou" (desfecho sobre a rede, que não pode).
    /// </remarks>
    public bool ProviderAnswered => Status == ExtractionStatus.Resolved || Status == ExtractionStatus.Empty;

    /// <summary>
    /// O modelo pode não achar instrumento e ainda assim ter lido o documento — valor,
    /// vencimento, beneficiário. É o retrato que alimenta o <c>DocumentReading</c>.
    /// </summary>
    private static bool HasReading(ExtractedDocument document)
        => document.PayerName is not null
            || document.PayerTaxId is not null
            || document.PayeeName is not null
            || document.PayeeTaxId is not null
            || document.Amount is not null
            || document.DueDate is not null
            || document.AccountReference is not null
            || document.BillingPeriod is not null
            || document.Description is not null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        yield return Document;
        yield return ReasonCode;
    }
}
