namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Baixa o comprovante do pagamento e o guarda no balde — o arquivo é a evidência, não a URL.
/// </summary>
/// <remarks>
/// <para>
/// A URL vem de um <c>GET</c> fresco no provedor (nunca é persistida — é credencial ao
/// portador e pode expirar). Falha transiente sobe como <c>BLP.PMO21</c> para a reentrega do
/// outbox retentar com backoff; "sem comprovante" é desfecho registrado, não erro.
/// </para>
/// <para>
/// <c>Definitive</c> separa as duas origens: o caminho do outbox (segundos após o pago —
/// comprovante pode ainda não existir; <c>false</c>) da rede de segurança da conciliação (a
/// segunda olhada, atrasada; <c>true</c>) — só a definitiva grava a marca que tira a ordem da
/// varredura para sempre.
/// </para>
/// </remarks>
public sealed record CapturePaymentReceiptCommand(Guid TenantId, Guid PaymentOrderId, bool Definitive = false)
    : ITenantScopedCommand, IRequest<CapturePaymentReceiptResponse>;

public sealed record CapturePaymentReceiptResponse(Guid PaymentOrderId, string Outcome);

public sealed class CapturePaymentReceiptCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    IPaymentReceiptFetcher receiptFetcher,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<CapturePaymentReceiptCommandHandler> logger)
    : IRequestHandler<CapturePaymentReceiptCommand, CapturePaymentReceiptResponse>
{
    private const string OUTCOME_STORED = "Stored";
    private const string OUTCOME_ALREADY_STORED = "AlreadyStored";
    private const string OUTCOME_NO_RECEIPT = "NoReceipt";
    private const string OUTCOME_SKIPPED = "Skipped";

    public async Task<CapturePaymentReceiptResponse> Handle(
        CapturePaymentReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken);
        if (order is null
            || (order.Status != PaymentOrderStatus.Paid && order.Status != PaymentOrderStatus.Refunded)
            || order.ProviderOrderId is null)
        {
            return new CapturePaymentReceiptResponse(request.PaymentOrderId, OUTCOME_SKIPPED);
        }

        if (!string.IsNullOrEmpty(order.ReceiptStorageKey))
            return new CapturePaymentReceiptResponse(request.PaymentOrderId, OUTCOME_ALREADY_STORED);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var credential = profile?.AsaasAccountRef;

        var fetch = await order.GetFromProviderAsync(
            billGateway, pixGateway, credential, order.ProviderOrderId, cancellationToken);

        if (fetch.IsUnavailable)
            throw PaymentOrderErrors.ReceiptUnavailable(fetch.ReasonCode);

        var receiptUrl = fetch.Snapshot?.ReceiptUrl;
        if (string.IsNullOrWhiteSpace(receiptUrl))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "A ordem {PaymentOrderId} está paga e o provedor não oferece comprovante.",
                    order.Id.Value);
            }

            return await RecordNoReceiptAsync(order, request, cancellationToken);
        }

        var receipt = await receiptFetcher.FetchAsync(receiptUrl, cancellationToken);

        if (receipt.IsRetryable)
            throw PaymentOrderErrors.ReceiptUnavailable(receipt.ReasonCode);

        if (!receipt.IsFetched)
        {
            logger.LogWarning(
                "O comprovante da ordem {PaymentOrderId} não pôde ser obtido ({Reason}).",
                order.Id.Value, receipt.ReasonCode);
            return await RecordNoReceiptAsync(order, request, cancellationToken);
        }

        var fileName = ReceiptFileName(order, receipt.ContentType);
        var storageKey = await storage.StoreAsync(
            tenantId, fileName, receipt.ContentType, receipt.Content!.Value, cancellationToken);

        try
        {
            order.AttachReceipt(storageKey, clock.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch
        {
            // O balde está FORA da transação do EF (o mesmo racional do ImportBill): sem a
            // limpeza, cada reentrega do outbox que perdesse o save deixaria um blob órfão.
            // CancellationToken.None de propósito — desistir da limpeza produz o órfão.
            await storage.RemoveAsync(tenantId, storageKey, CancellationToken.None);
            throw;
        }

        return new CapturePaymentReceiptResponse(request.PaymentOrderId, OUTCOME_STORED);
    }

    /// <summary>
    /// "Sem comprovante" com a origem decidindo a persistência: a olhada DEFINITIVA (rede de
    /// segurança, atrasada) grava a marca que tira a ordem da varredura; a do outbox (segundos
    /// após o pago — o arquivo pode só não existir AINDA) deixa a varredura reconferir depois.
    /// </summary>
    private async Task<CapturePaymentReceiptResponse> RecordNoReceiptAsync(
        PaymentOrder order,
        CapturePaymentReceiptCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Definitive)
        {
            order.MarkReceiptMissing(clock.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        return new CapturePaymentReceiptResponse(request.PaymentOrderId, OUTCOME_NO_RECEIPT);
    }

    private static string ReceiptFileName(PaymentOrder order, string? contentType)
    {
        var extension = contentType?.ToUpperInvariant() switch
        {
            "APPLICATION/PDF" => "pdf",
            "IMAGE/PNG" => "png",
            "IMAGE/JPEG" => "jpg",
            "TEXT/HTML" => "html",
            _ => "bin",
        };

        return $"comprovante-{order.Id.Value}.{extension}";
    }
}
