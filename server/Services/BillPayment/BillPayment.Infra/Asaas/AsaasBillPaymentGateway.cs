namespace BillPayment.Infra.Asaas;

using System.Globalization;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Agenda, consulta e cancela o pague-contas em <c>POST /v3/bill</c> e vizinhos.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Usa o cliente SEM retry</strong> (<see cref="AsaasHttp.PAYMENT_CLIENT_NAME"/>) — a
/// única exceção é a busca por referência, que é read-only mas compartilha o cliente para não
/// existirem dois caminhos de configuração para o mesmo endpoint de dinheiro.
/// </para>
/// <para>
/// <strong>Nada aqui loga linha digitável, valor ou URL de comprovante</strong> — instrumento
/// de pagamento e credencial ao portador. O log carrega caminho, status e motivo.
/// </para>
/// </remarks>
internal sealed class AsaasBillPaymentGateway(
    AsaasClientProvider clientProvider,
    ILogger<AsaasBillPaymentGateway> logger) : IBillPaymentGateway
{
    private const string BILL_PATH = "bill";

    public async Task<PaymentSubmissionResult> ScheduleAsync(
        CredentialRef? credential,
        DigitableLine digitableLine,
        Money amount,
        DateOnly? dueDate,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(digitableLine);
        ArgumentNullException.ThrowIfNull(amount);

        var (http, reasonCode, message) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return PaymentSubmissionResult.Unavailable(reasonCode!, message);

        using var _ = http;
        var payload = new
        {
            identificationField = digitableLine.Value,
            value = amount.Amount,
            dueDate = dueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            scheduleDate = scheduleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description,
            externalReference,
        };

        var (body, failure) = await http.PostAsync<AsaasBillPaymentResponse>(
            BILL_PATH, payload, logger, cancellationToken);

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
        var path = $"{BILL_PATH}?externalReference={Uri.EscapeDataString(externalReference)}";
        var (body, failure) = await http.GetAsync<AsaasBillPaymentListResponse>(path, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentFetchResult.Unavailable(failure.ReasonCode)
                : PaymentFetchResult.NotFound();

        var first = body!.Data?.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.Id));
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
        var path = $"{BILL_PATH}/{Uri.EscapeDataString(providerOrderId)}";
        var (body, failure) = await http.GetAsync<AsaasBillPaymentResponse>(path, logger, cancellationToken);

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
        var path = $"{BILL_PATH}/{Uri.EscapeDataString(providerOrderId)}/cancel";
        var (body, failure) = await http.PostAsync<AsaasBillPaymentResponse>(
            path, new { }, logger, cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PaymentCancellationResult.Unavailable(failure.ReasonCode)
                : PaymentCancellationResult.Refused(failure.ReasonCode);

        return AsaasPaymentStatusMap.FromBillPayment(body!.Status) == PaymentOrderStatus.Cancelled
            ? PaymentCancellationResult.Cancelled()
            : PaymentCancellationResult.Refused("not_cancellable");
    }
}
