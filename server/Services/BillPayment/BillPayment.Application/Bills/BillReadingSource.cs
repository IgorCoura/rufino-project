namespace BillPayment.Application.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;

/// <param name="Status">
/// O desfecho da tentativa. <c>Unavailable</c> e <c>BudgetExhausted</c> pedem retentativa; o
/// resto é fato sobre o documento.
/// </param>
/// <param name="Reading">O retrato, quando houve leitura com conteúdo.</param>
/// <param name="ReasonCode">Código estável do motivo, quando não houve.</param>
public sealed record BillReadingOutcome(ExtractionStatus Status, DocumentReading? Reading, string? ReasonCode)
{
    public bool HasReading => Reading is not null;
}

/// <summary>
/// Relê o documento original de um boleto pelo extrator de IA.
/// </summary>
/// <remarks>
/// <strong>Existe para os dois caminhos compartilharem uma implementação só.</strong> A fila de
/// análise e o pedido manual de reler fazem exatamente o mesmo trabalho — abrir o documento
/// guardado, buscar o corpo do e-mail no livro-caixa, montar as dicas do próprio tenant e chamar
/// o extrator. Duas cópias divergiriam, e a divergência apareceria como "pela fila lê, pelo botão
/// não".
/// </remarks>
public interface IBillReadingSource
{
    Task<BillReadingOutcome> ReadAsync(Bill bill, TenantId tenantId, CancellationToken cancellationToken);
}

internal sealed class BillReadingSource(
    IPayerProfileRepository payerProfiles,
    IPayeeRepository payees,
    ICapturedMessageRepository capturedMessages,
    IAttachmentStorage storage,
    IBoletoDocumentParser parser,
    IDocumentIntelligence documentIntelligence,
    TimeProvider clock) : IBillReadingSource
{
    public async Task<BillReadingOutcome> ReadAsync(
        Bill bill,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bill);

        if (!documentIntelligence.IsEnabled)
            return None("intelligence_disabled");

        // Boleto importado só com os dígitos não tem documento para ler — ausência, não erro.
        if (string.IsNullOrEmpty(bill.Origin.StorageKey))
            return None("no_stored_document");

        using var artifact = await storage.OpenAsync(tenantId, bill.Origin.StorageKey, cancellationToken);
        if (artifact is null)
            return None("document_unavailable");

        using var buffer = new MemoryStream();
        await artifact.Content.CopyToAsync(buffer, cancellationToken);
        var content = new ReadOnlyMemory<byte>(buffer.ToArray());

        if (content.IsEmpty)
            return None("document_unavailable");

        if (!DocumentPayload.IsSupported(artifact.ContentType))
            return None("unsupported_media_type");

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);

        // O documento guardado é o ORIGINAL, e original de emissor que protege por senha vem
        // cifrado — o extrator recusa esses bytes. A cópia sem senha é produzida aqui pelo mesmo
        // parser da captura; documento que já abre devolve nulo e segue como está.
        var clear = await parser.UnlockAsync(
            content, artifact.ContentType, PasswordDerivationService.Derive(profile), cancellationToken);

        if (clear is { } unlocked)
            content = unlocked;

        var body = await LoadBodyTextAsync(bill, tenantId, cancellationToken);
        var hints = await BuildHintsAsync(bill, profile, tenantId, cancellationToken);

        var attempt = await documentIntelligence.ExtractAsync(
            DocumentPayload.From(tenantId, content, artifact.ContentType, body?.Text, body?.IsHtml ?? false),
            hints,
            cancellationToken);

        // Indisponibilidade e cota atravessam com o status intacto: é quem chama que decide se
        // devolve o boleto à fila. Colapsá-las aqui em "nada extraído" reintroduziria a confusão
        // que mandava documento bom para a quarentena por 503.
        if (attempt.IsRetryable)
            return new BillReadingOutcome(attempt.Status, Reading: null, attempt.ReasonCode);

        var reading = DocumentReading.FromExtraction(attempt.Document, clock.GetUtcNow());

        return reading.HasContent
            ? new BillReadingOutcome(attempt.Status, reading, ReasonCode: null)
            : new BillReadingOutcome(attempt.Status, Reading: null, attempt.ReasonCode ?? "nothing_extracted");
    }

    /// <summary>Ausência determinística: nada a ler, e retentar daria o mesmo.</summary>
    private static BillReadingOutcome None(string reasonCode)
        => new(ExtractionStatus.Empty, Reading: null, reasonCode);

    private async Task<(string Text, bool IsHtml)?> LoadBodyTextAsync(
        Bill bill,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        if (bill.Origin.SourceId is not { } sourceId || string.IsNullOrEmpty(bill.Origin.ExternalMessageId))
            return null;

        var message = await capturedMessages.FindByExternalMessageIdAsync(
            tenantId, CaptureSourceId.From(sourceId), bill.Origin.ExternalMessageId, cancellationToken);

        if (message is null || !message.HasStoredBody)
            return null;

        var stored = await storage.RetrieveAsync(tenantId, message.BodyStorageKey!, cancellationToken);
        if (stored.IsEmpty)
            return null;

        var isHtml = message.BodyContentType?.Contains("html", StringComparison.OrdinalIgnoreCase) ?? true;
        return (System.Text.Encoding.UTF8.GetString(stored.Span), isHtml);
    }

    /// <summary>Só dado do próprio tenant sai daqui — mesma regra do processamento de captura.</summary>
    private async Task<ExtractionHints> BuildHintsAsync(
        Bill bill,
        PayerProfile? profile,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var taxIds = profile is null
            ? []
            : new[] { profile.PrimaryTaxId.Value }
                .Concat(profile.AdditionalTaxIds.Select(t => t.Value))
                .ToList();

        var knownPayees = await payees.ListByTenantAsync(tenantId, cancellationToken);

        return ExtractionHints.From(
            taxIds,
            knownPayees.Select(p => p.LegalName),
            bill.Origin.SenderAddress);
    }
}
