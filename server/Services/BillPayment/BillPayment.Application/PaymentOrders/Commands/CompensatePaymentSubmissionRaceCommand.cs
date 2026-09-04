namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// A compensação da corrida cancelar × submeter: a submissão foi aceita pelo provedor, mas o
/// save do worker perdeu a corrida para um cancelamento local — a ordem aqui está terminal e o
/// pagamento pode estar VIVO lá. Consulta por <c>externalReference</c> e, achando, cancela no
/// provedor (best-effort).
/// </summary>
/// <remarks>
/// A guarda do <c>BLP.PMO22</c> fecha a janela pela frente (cancelar espera o aluguel vencer);
/// este comando é o cinto de trás, para a janela que sobrar. Nunca lança para o chamador: o
/// desfecho — compensado, recusado, indisponível — sai em log de erro e no canal operacional
/// (ADR-014), porque dinheiro vivo para ordem cancelada jamais pode ficar em silêncio.
/// </remarks>
public sealed record CompensatePaymentSubmissionRaceCommand(Guid TenantId, Guid PaymentOrderId)
    : ITenantScopedCommand, IRequest<CompensatePaymentSubmissionRaceResponse>;

public sealed record CompensatePaymentSubmissionRaceResponse(Guid PaymentOrderId, string Outcome);

public sealed class CompensatePaymentSubmissionRaceCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    INotificationSender notifications,
    ILogger<CompensatePaymentSubmissionRaceCommandHandler> logger)
    : IRequestHandler<CompensatePaymentSubmissionRaceCommand, CompensatePaymentSubmissionRaceResponse>
{
    private const string OUTCOME_SKIPPED = "Skipped";
    private const string OUTCOME_NOTHING_AT_PROVIDER = "NothingAtProvider";
    private const string OUTCOME_CANCELLED_AT_PROVIDER = "CancelledAtProvider";
    private const string OUTCOME_LIVE_AT_PROVIDER = "LiveAtProvider";

    public async Task<CompensatePaymentSubmissionRaceResponse> Handle(
        CompensatePaymentSubmissionRaceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(
            tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken);

        // Só ordem CANCELADA compensa: Failed/Refunded têm as próprias trilhas, e uma ordem viva
        // não perdeu corrida nenhuma.
        if (order is null || order.Status != PaymentOrderStatus.Cancelled)
            return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_SKIPPED);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var credential = profile?.AsaasAccountRef;

        var fetch = await order.FindByExternalReferenceAsync(
            billGateway, pixGateway, credential, cancellationToken);

        if (fetch.IsUnavailable)
        {
            await AlarmAsync(order, "o provedor está indisponível para conferir", cancellationToken);
            return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_LIVE_AT_PROVIDER);
        }

        if (!fetch.IsFound)
            return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_NOTHING_AT_PROVIDER);

        var snapshot = fetch.Snapshot!;
        if (snapshot.Status == PaymentOrderStatus.Cancelled || snapshot.Status == PaymentOrderStatus.Refunded)
            return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_NOTHING_AT_PROVIDER);

        var cancel = await order.CancelAtProviderAsync(
            billGateway, pixGateway, credential, snapshot.ProviderOrderId, cancellationToken);

        if (cancel.IsCancelled)
        {
            logger.LogError(
                "Corrida cancelar×submeter compensada: a ordem {PaymentOrderId} estava VIVA no provedor "
                + "({ProviderOrderId}) e foi cancelada lá.",
                order.Id.Value, snapshot.ProviderOrderId);

            return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_CANCELLED_AT_PROVIDER);
        }

        await AlarmAsync(
            order,
            $"o provedor recusou o cancelamento ({cancel.ReasonCode ?? "sem motivo"})",
            cancellationToken);

        return new CompensatePaymentSubmissionRaceResponse(request.PaymentOrderId, OUTCOME_LIVE_AT_PROVIDER);
    }

    /// <summary>Dinheiro possivelmente vivo para ordem cancelada: log de erro + canal do ADR-014.</summary>
    private async Task AlarmAsync(PaymentOrder order, string detail, CancellationToken cancellationToken)
    {
        logger.LogError(
            "PAGAMENTO POSSIVELMENTE VIVO no provedor para a ordem CANCELADA {PaymentOrderId}: {Detail}. "
            + "Verifique a conta no provedor.",
            order.Id.Value, detail);

        await notifications.SendAsync(
            order.TenantId,
            NotificationKind.PaymentFailed,
            new NotificationPayload(
                "Um pagamento cancelado pode estar ativo no provedor",
                "Um agendamento foi cancelado aqui, mas o provedor pode tê-lo aceitado. Verifique a conta no provedor e, se necessário, cancele por lá.",
                $"/bill-payment/bills/{order.BillId.Value}"),
            cancellationToken);
    }
}
