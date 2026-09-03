namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Options;

/// <summary>
/// A fila de submissão executa UMA ordem: resolve a data efetiva (regras do provedor + política
/// do ADR-017), confere a idempotência por <c>externalReference</c>, e só então fala com o
/// gateway do trilho.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A retentativa começa pela consulta, nunca pelo reenvio.</strong> A partir da segunda
/// passagem — a primeira pode ter morrido depois de submeter — o handler pergunta ao provedor
/// pela referência; achando, adota a ordem que já existe lá. É o que transforma "timeout na
/// criação" em uma ordem, não duas.
/// </para>
/// <para>
/// <strong>Indisponibilidade sobe como <c>BLP.PMO18</c></strong> para o worker devolver a ordem
/// à fila com espera dobrando — o mesmo desenho do <c>BLP.BIL28</c> da leitura por IA. Recusa do
/// provedor NÃO sobe: vira desistência visível (<c>Failed</c> + evento), porque repetir a mesma
/// ordem contra a mesma recusa só gastaria a janela da política.
/// </para>
/// </remarks>
public sealed record SubmitPaymentOrderCommand(Guid TenantId, Guid PaymentOrderId)
    : ITenantScopedCommand, IRequest<SubmitPaymentOrderResponse>;

public sealed record SubmitPaymentOrderResponse(Guid PaymentOrderId, string Outcome);

public sealed class SubmitPaymentOrderCommandHandler(
    IPaymentOrderRepository orders,
    IBillRepository bills,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    IWorkingDayCalendar calendar,
    IOptions<PaymentSchedulingOptions> options,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitPaymentOrderCommand, SubmitPaymentOrderResponse>
{
    private const string OUTCOME_SUBMITTED = "Submitted";
    private const string OUTCOME_ALREADY_SUBMITTED = "AlreadySubmitted";
    private const string OUTCOME_HELD = "Held";
    private const string OUTCOME_REFUSED = "Refused";

    public async Task<SubmitPaymentOrderResponse> Handle(
        SubmitPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken)
            ?? throw PaymentOrderErrors.NotFound();

        if (order.Status != PaymentOrderStatus.Draft)
            return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_ALREADY_SUBMITTED);

        if (order.Hold != PaymentOrderHold.None)
            return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_HELD);

        var nowUtc = clock.GetUtcNow();
        var now = nowUtc.UtcDateTime;

        var bill = await bills.GetAsync(tenantId, order.BillId, cancellationToken);
        if (bill is null)
            return await RefuseAsync(order, "bill_missing", now, cancellationToken);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        if (profile is null || !profile.CanSchedulePayments)
        {
            order.HoldForMissingAccount(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_HELD);
        }

        var credential = profile.AsaasAccountRef;

        // Da segunda passagem em diante, a primeira pergunta é ao provedor: a primeira tentativa
        // pode ter morrido DEPOIS de submeter, e reenviar sem conferir é o pagamento duplicado
        // que o doc 04 manda impedir.
        if (order.SubmissionAttempts > 1)
        {
            var fetch = await order.FindByExternalReferenceAsync(
                billGateway, pixGateway, credential, cancellationToken);

            if (fetch.IsUnavailable)
                throw PaymentOrderErrors.SubmissionUnavailable(fetch.ReasonCode);

            if (fetch.IsFound)
            {
                Adopt(order, fetch.Snapshot!, nowUtc, now);
                await unitOfWork.SaveEntitiesAsync(cancellationToken);
                return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_SUBMITTED);
            }
        }

        var schedulingOptions = options.Value;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(now, schedulingOptions.ResolveTimeZone());

        var resolution = PaymentSchedulingService.Resolve(
            order.RequestedScheduleDate,
            bill.DueDate,
            bill.Lookup?.MinimumScheduleDate,
            nowLocal,
            schedulingOptions.ToPolicy(),
            calendar);

        // Vencido sem consentimento: a fila para e pergunta — nunca paga na hora em silêncio
        // (ADR-017). O consentimento dado na aprovação viaja na ordem e passa direto por aqui.
        if (resolution.RequiresImmediateExecution && !order.HasImmediateExecutionConsent)
        {
            order.HoldForConfirmation(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_HELD);
        }

        // A guarda do valor é do agregado (PMO10): DomainException sobe, o worker a classifica
        // como permanente e a ordem desiste com falha visível — nunca vai ao gateway sem valor.
        var amount = order.Amount ?? bill.AmountForPayment;
        order.EnsureSubmittable(amount);

        var result = order.Rail == PaymentRail.Pix
            ? await SubmitPixAsync(order, bill, credential, amount!, resolution, cancellationToken)
            : await SubmitBankSlipAsync(order, bill, credential, amount!, resolution, cancellationToken);

        if (!result.IsAccepted)
        {
            if (result.IsRetryable)
                throw PaymentOrderErrors.SubmissionUnavailable(result.ReasonCode);

            var reason = string.IsNullOrWhiteSpace(result.ProviderMessage)
                ? result.ReasonCode ?? "refused"
                : $"{result.ReasonCode}: {result.ProviderMessage}";

            return await RefuseAsync(order, reason, now, cancellationToken);
        }

        Adopt(order, result.Snapshot!, nowUtc, now, amount, resolution.EffectiveDate);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new SubmitPaymentOrderResponse(request.PaymentOrderId, OUTCOME_SUBMITTED);
    }

    private Task<PaymentSubmissionResult> SubmitBankSlipAsync(
        PaymentOrder order,
        Bill bill,
        Domain.Secrets.CredentialRef? credential,
        Money amount,
        SchedulingResolution resolution,
        CancellationToken cancellationToken)
    {
        var line = bill.Instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Select(i => i.DigitableLine)
            .FirstOrDefault();

        return line is null
            ? Task.FromResult(PaymentSubmissionResult.Refused("instrument_missing", null))
            : billGateway.ScheduleAsync(
                credential, line, amount, bill.DueDate, resolution.EffectiveDate,
                order.ExternalReference, description: null, cancellationToken);
    }

    private Task<PaymentSubmissionResult> SubmitPixAsync(
        PaymentOrder order,
        Bill bill,
        Domain.Secrets.CredentialRef? credential,
        Money amount,
        SchedulingResolution resolution,
        CancellationToken cancellationToken)
    {
        var payload = bill.Instruments
            .Where(i => i.Kind == PaymentInstrumentKind.PixQr)
            .Select(i => i.PixPayload)
            .FirstOrDefault();

        return payload is null
            ? Task.FromResult(PaymentSubmissionResult.Refused("instrument_missing", null))
            : pixGateway.PayAsync(
                credential, payload, amount, resolution.EffectiveDate,
                order.ExternalReference, description: null, cancellationToken);
    }

    /// <summary>
    /// Assume o retrato do provedor: submissão aceita agora, ou a que uma tentativa anterior
    /// deixou lá. Estado além de <c>Pending</c> é aplicado em seguida — um Pix imediato pode
    /// voltar já <c>DONE</c>.
    /// </summary>
    private static void Adopt(
        PaymentOrder order,
        ProviderPaymentSnapshot snapshot,
        DateTimeOffset syncedAt,
        DateTime now,
        Money? amount = null,
        DateOnly? resolvedDate = null)
    {
        var effective = snapshot.EffectiveScheduleDate
            ?? resolvedDate
            ?? snapshot.PaidAt
            ?? DateOnly.FromDateTime(now);

        // Na adoção pura (retentativa que encontrou a ordem lá) o valor vem do retrato do
        // provedor — sem ele, uma ordem paga apareceria sem valor no relatório.
        order.MarkSubmitted(snapshot.ProviderOrderId, effective, amount ?? snapshot.Amount, snapshot.Fee, now);

        if (snapshot.Status != PaymentOrderStatus.Pending)
        {
            order.ApplyProviderStatus(
                snapshot.Status, snapshot.PaidAt, snapshot.Fee, snapshot.FailReasons, syncedAt, now);
        }
    }

    /// <summary>Recusa permanente: desistir É o desfecho, e persistir é o que tira a ordem da fila.</summary>
    private async Task<SubmitPaymentOrderResponse> RefuseAsync(
        PaymentOrder order,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        order.RecordSubmissionFailure(permanent: true, reason, maxAttempts: 0, TimeSpan.Zero, now);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new SubmitPaymentOrderResponse(order.Id.Value, OUTCOME_REFUSED);
    }
}
