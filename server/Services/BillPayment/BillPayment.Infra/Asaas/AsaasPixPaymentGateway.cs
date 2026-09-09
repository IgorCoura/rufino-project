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
/// <strong>MEDIDO EM SANDBOX (2026-09-08) — o provedor DESCARTA o <c>externalReference</c>.</strong>
/// Enviamos o campo no corpo e a resposta volta com <c>"externalReference": null</c>, tanto na
/// transação quanto no <c>transfer</c> espelho. O doc 04 supunha que ele fosse "campo de busca,
/// não chave de deduplicação"; a medição é pior que a suposição — ele não é nem campo de busca,
/// porque não é gravado.
/// </para>
/// <para>
/// <strong>E o filtro <c>?externalReference=</c> é IGNORADO pelo servidor:</strong> uma consulta
/// com GUID inexistente devolveu a lista inteira (<c>totalCount=3</c>). Quem confiasse nele
/// pegaria <c>data[0]</c> — a transação de OUTRO pagamento — e a adotaria como sendo a nossa.
/// </para>
/// <para>
/// <strong>A saída é o <c>description</c></strong>, que o provedor grava e devolve: mandamos
/// nele um marcador com o id da ordem, e a busca lista as transações recentes e casa
/// <strong>localmente</strong>. Não achando dentro da janela varrida, a resposta é
/// <c>Unavailable</c> — nunca <c>NotFound</c>: "não achei numa lista que não sei se é completa"
/// não autoriza reenviar dinheiro. Quem trata isso é o hold de reconciliação manual.
/// </para>
/// </remarks>
internal sealed class AsaasPixPaymentGateway(
    AsaasClientProvider clientProvider,
    ILogger<AsaasPixPaymentGateway> logger) : IPixPaymentGateway
{
    private const string PAY_PATH = "pix/qrCodes/pay";
    private const string TRANSACTIONS_PATH = "pix/transactions";

    /// <summary>
    /// Prefixo do marcador que viaja no <c>description</c> — o único campo do trilho Pix que o
    /// provedor comprovadamente persiste e devolve.
    /// </summary>
    internal const string REFERENCE_MARKER_PREFIX = "RUF:";

    /// <summary>Quantas páginas de transações a busca varre antes de desistir.</summary>
    private const int REFERENCE_SCAN_MAX_PAGES = 5;

    private const int REFERENCE_SCAN_PAGE_SIZE = 100;

    internal static string BuildReferenceMarker(string externalReference)
        => $"{REFERENCE_MARKER_PREFIX}{externalReference}";

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

        // O marcador vai no description porque é o ÚNICO campo que o provedor comprovadamente
        // grava e devolve neste endpoint (o externalReference some). A descrição do chamador,
        // quando existe, vem depois — o marcador na frente mantém o prefixo estável para o
        // casamento, sem depender do que alguém escreveu.
        var marker = BuildReferenceMarker(externalReference);
        var request = new
        {
            qrCode = new { payload = payload.Payload },
            value = amount.Amount,
            scheduleDate = scheduleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description = string.IsNullOrWhiteSpace(description) ? marker : $"{marker} {description}",
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

        // NÃO se usa o filtro do servidor: ele é ignorado (medido em 2026-09-08) e devolveria a
        // lista inteira, cujo primeiro item é a transação de outro pagamento. Varre-se a janela
        // recente e o casamento é LOCAL, pelo marcador que PayAsync gravou no description.
        var marker = BuildReferenceMarker(externalReference);

        for (var page = 0; page < REFERENCE_SCAN_MAX_PAGES; page++)
        {
            var path = $"{TRANSACTIONS_PATH}?limit={REFERENCE_SCAN_PAGE_SIZE}"
                + $"&offset={page * REFERENCE_SCAN_PAGE_SIZE}";

            var (body, failure) = await http.GetAsync<AsaasPixPaymentListResponse>(path, logger, cancellationToken);

            // Qualquer falha trava o reenvio: NotFound indevido aqui é pagamento duplicado.
            if (failure is not null)
                return PaymentFetchResult.Unavailable(failure.ReasonCode);

            var data = body!.Data ?? [];

            var match = data.Find(t =>
                !string.IsNullOrWhiteSpace(t.Id)
                && t.Description is not null
                && t.Description.StartsWith(marker, StringComparison.Ordinal));

            if (match is not null)
                return PaymentFetchResult.Found(AsaasPaymentStatusMap.ToSnapshot(match));

            // Fim da lista sem casar: a janela varrida É completa, então a ausência é definitiva
            // e autoriza o reenvio.
            if (data.Count < REFERENCE_SCAN_PAGE_SIZE)
                return PaymentFetchResult.NotFound();
        }

        // Teto de páginas esgotado com lista ainda cheia: não dá para AFIRMAR que a transação não
        // existe, e "não sei" nunca autoriza reenviar dinheiro. A fila retém para conferência
        // humana em vez de arriscar o pagamento em dobro.
        logger.LogWarning(
            "Busca Pix por referência varreu {Pages} páginas sem casar o marcador e sem chegar ao fim da lista.",
            REFERENCE_SCAN_MAX_PAGES);

        return PaymentFetchResult.Unavailable("reference_scan_exhausted");
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

        // Só o 404 afirma ausência; o resto degrada para Unavailable e a conciliação segue.
        if (failure is not null)
            return failure.IsNotFound
                ? PaymentFetchResult.NotFound()
                : PaymentFetchResult.Unavailable(failure.ReasonCode);

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
