namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Um boleto chegou e foi verificado: procura o ciclo que ele cumpre e o fecha.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Roda depois da verificação, não da captura</strong>, porque é a verificação que
/// resolve o beneficiário (<c>Bill.PayeeId</c>) — e sem beneficiário não há contra o quê casar.
/// </para>
/// <para>
/// <strong>Boleto que chega antes de o ciclo existir abre o próprio ciclo.</strong> É a rede de
/// segurança contra prazo de chegada subestimado: o ciclo nasce <c>ObservedLeadDays</c> antes do
/// vencimento, e uma conta que chegue antes disso não teria o que cumprir — viraria alerta de
/// "não chegou" sobre um boleto capturado e aprovado. Quem escolhe a expectativa é o Domain
/// Service, e ele recusa quando há mais de uma candidata.
/// </para>
/// <para>
/// <strong>Não achar ciclo é desfecho normal</strong>, não erro: a maior parte dos boletos não
/// tem expectativa nenhuma, e um boleto ambíguo entre duas contas do mesmo beneficiário também
/// não fecha ciclo — cumprir a expectativa errada apagaria o alerta da conta que de fato não
/// chegou, que é a falha silenciosa que tudo isto existe para impedir.
/// </para>
/// </remarks>
public sealed record FulfillExpectationForBillCommand(Guid TenantId, Guid BillId)
    : ITenantScopedCommand, IRequest<FulfillExpectationForBillResponse>;

public sealed record FulfillExpectationForBillResponse(Guid BillId, Guid? ExpectationId, Guid? CycleId);

public sealed class FulfillExpectationForBillCommandHandler(
    IBillRepository bills,
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<FulfillExpectationForBillCommandHandler> logger)
    : IRequestHandler<FulfillExpectationForBillCommand, FulfillExpectationForBillResponse>
{
    public async Task<FulfillExpectationForBillResponse> Handle(
        FulfillExpectationForBillCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var bill = await bills.GetAsync(tenantId, BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        if (bill.PayeeId is not { } payeeId)
            return NoMatch(request.BillId);

        var candidates = await expectations.ListByPayeeAsync(tenantId, payeeId, cancellationToken);
        if (candidates.Count == 0)
            return NoMatch(request.BillId);

        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        if (bill.DueDate is not { } dueDate)
            return NoMatch(request.BillId);

        var competence = new CompetencePeriod(dueDate.Year, dueDate.Month);
        var arrivedOn = DateOnly.FromDateTime(bill.Origin.ReceivedAt);
        var arrivedThrough = SourceOf(bill);

        var match = ExpectationMatchingService.Match(candidates, dueDate, today);

        if (match is not null)
        {
            var matched = candidates.First(e => e.Id == match.ExpectationId);

            // Reentrega do outbox: um ciclo já cumprido recusa novo cumprimento pela própria
            // máquina de estados, então basta não insistir.
            if (matched.CycleFor(competence)?.Status == CycleStatus.Fulfilled)
                return new FulfillExpectationForBillResponse(request.BillId, match.ExpectationId.Value, match.CycleId.Value);

            matched.Fulfill(match.CycleId, bill.Id, dueDate, arrivedOn, arrivedThrough, now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            return new FulfillExpectationForBillResponse(
                request.BillId, match.ExpectationId.Value, match.CycleId.Value);
        }

        var soleId = ExpectationMatchingService.SoleWatchingWithoutCycleFor(candidates, competence, today);

        if (soleId is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Boleto verificado não casou com nenhum ciclo aberto entre {Count} expectativas do beneficiário.",
                    candidates.Count);
            }

            return NoMatch(request.BillId);
        }

        var expectation = candidates.First(e => e.Id == soleId.Value);
        var opened = expectation.OpenCycle(competence, now);

        expectation.Fulfill(opened.Id, bill.Id, dueDate, arrivedOn, arrivedThrough, now);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new FulfillExpectationForBillResponse(
            request.BillId, expectation.Id.Value, opened.Id.Value);
    }

    private static FulfillExpectationForBillResponse NoMatch(Guid billId)
        => new(billId, null, null);

    /// <summary>
    /// Por onde o boleto chegou. Alimenta o <c>HintSourceId</c> da expectativa, que é o que liga
    /// um artefato travado à conta que ele seria — importação manual não tem fonte, e aí o hint
    /// anterior é preservado.
    /// </summary>
    private static CaptureSourceId? SourceOf(Bill bill)
        => bill.Origin.SourceId is { } sourceId ? CaptureSourceId.From(sourceId) : null;
}

public sealed class FulfillExpectationForBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<FulfillExpectationForBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<FulfillExpectationForBillCommand, FulfillExpectationForBillResponse>(
        mediator, requestManager, logger)
{
    protected override FulfillExpectationForBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, null, null);
}
