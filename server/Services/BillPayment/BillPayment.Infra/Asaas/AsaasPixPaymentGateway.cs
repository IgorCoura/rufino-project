namespace BillPayment.Infra.Asaas;

using System.Globalization;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Paga o QR Pix agendado em <c>POST /v3/pix/qrCodes/pay</c> e acompanha a transação.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este endpoint não documenta idempotência</strong> — o <c>externalReference</c> dele é
/// campo de busca, não chave de deduplicação (doc 04). É por isso que o cliente é o SEM retry e
/// a fila de submissão confere por referência antes de qualquer reenvio: sem essas duas coisas,
/// o trilho que o ADR-010 prefere seria o mais arriscado.
/// </para>
/// <para>
/// <strong>Ressalva medida ainda não</strong>: a busca por <c>externalReference</c> em
/// <c>GET /v3/pix/transactions</c> é a forma plausível do contrato e está bloqueada na sonda de
/// sandbox (sem chave). Falha dela degrada para <c>Unavailable</c> — que trava o reenvio, o
/// lado seguro.
/// </para>
/// </remarks>
internal sealed class AsaasPixPaymentGateway(
    AsaasClientProvider clientProvider,
    ILogger<AsaasPixPaymentGateway> logger) : IPixPaymentGateway
{
    private const string PAY_PATH = "pix/qrCodes/pay";
    private const string TRANSACTIONS_PATH = "pix/transactions";

    public async Task<PaymentSubmissionResult> PayAsync(
        CredentialRef? credential,
        PixPayload payload,
        Money amount,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(amount);

        var (http, reasonCode, message) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return PaymentSubmissionResult.Unavailable(reasonCode!, message);

        using var _ = http;
        var request = new
        {
            qrCode = new { payload = payload.Payload },
            value = amount.Amount,
            scheduleDate = scheduleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description,
            externalReference,
        };

        var (body, failure) = await http.PostAsync<AsaasPixPaymentResponse>(
            PAY_PATH, request, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentSubmissionResult.Unavailable(failure.ReasonCode, failure.Message)
                : PaymentSubmissionResult.Refused(failure.ReasonCode, failure.Message);

        return string.IsNullOrWhiteSpace(body!.Id)
            ? PaymentSubmissionResult.Refused("missing_provider_id", null)
            : PaymentSubmissionResult.Accepted(AsaasPaymentStatusMap.ToSnapshot(body));
    }

    public async Task<PaymentFetchResult> FindByExternalReferenceAsync(
        CredentialRef? credential,
        string externalReference,
        CancellationToken cancellationToken)
    {
        var (http, reasonCode, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return PaymentFetchResult.Unavailable(reasonCode!);

        using var _ = http;
        var path = $"{TRANSACTIONS_PATH}?externalReference={Uri.EscapeDataString(externalReference)}";
        var (body, failure) = await http.GetAsync<AsaasPixPaymentListResponse>(path, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentFetchResult.Unavailable(failure.ReasonCode)
                : PaymentFetchResult.NotFound();

        var first = body!.Data?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Id));
        return first is null
            ? PaymentFetchResult.NotFound()
            : PaymentFetchResult.Found(AsaasPaymentStatusMap.ToSnapshot(first));
    }

    public async Task<PaymentFetchResult> GetAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
    {
        var (http, reasonCode, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return PaymentFetchResult.Unavailable(reasonCode!);

        using var _ = http;
        var path = $"{TRANSACTIONS_PATH}/{Uri.EscapeDataString(providerOrderId)}";
        var (body, failure) = await http.GetAsync<AsaasPixPaymentResponse>(path, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentFetchResult.Unavailable(failure.ReasonCode)
                : PaymentFetchResult.NotFound();

        return string.IsNullOrWhiteSpace(body!.Id)
            ? PaymentFetchResult.NotFound()
            : PaymentFetchResult.Found(AsaasPaymentStatusMap.ToSnapshot(body));
    }

    public async Task<PaymentCancellationResult> CancelAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken)
    {
        var (http, reasonCode, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return PaymentCancellationResult.Unavailable(reasonCode!);

        using var _ = http;
        var path = $"{TRANSACTIONS_PATH}/{Uri.EscapeDataString(providerOrderId)}/cancel";
        var (body, failure) = await http.PostAsync<AsaasPixPaymentResponse>(
            path, new { }, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentCancellationResult.Unavailable(failure.ReasonCode)
                : PaymentCancellationResult.Refused(failure.ReasonCode);

        return AsaasPaymentStatusMap.FromPixPayment(body!.Status) == PaymentOrderStatus.Cancelled
            ? PaymentCancellationResult.Cancelled()
            : PaymentCancellationResult.Refused("not_cancellable");
    }
}
