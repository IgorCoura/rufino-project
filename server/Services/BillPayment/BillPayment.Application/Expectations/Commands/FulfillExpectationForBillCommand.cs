namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
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
/// <strong>Não achar ciclo é desfecho normal</strong>, não erro: a maior parte dos boletos não
/// tem expectativa nenhuma, e um boleto ambíguo entre duas contas do mesmo beneficiário também
/// não fecha ciclo — cumprir a expectativa errada apagaria o alerta da conta que de fato não
/// chegou, que é a falha silenciosa que tudo isto existe para impedir.
/// </para>
/// </remarks>
public sealed record FulfillExpectationForBillCommand(Guid TenantId, Guid BillId)
    : IRequest<FulfillExpectationForBillResponse>;

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
            return new FulfillExpectationForBillResponse(request.BillId, null, null);

        var candidates = await expectations.ListByPayeeAsync(tenantId, payeeId, cancellationToken);
        if (candidates.Count == 0)
            return new FulfillExpectationForBillResponse(request.BillId, null, null);

        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var dueDate = DueDateOf(bill);

        var match = ExpectationMatchingService.Match(candidates, dueDate, today);

        if (match is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Boleto verificado não casou com nenhum ciclo aberto entre {Count} expectativas do beneficiário.",
                    candidates.Count);
            }

            return new FulfillExpectationForBillResponse(request.BillId, null, null);
        }

        var expectation = candidates.First(e => e.Id == match.ExpectationId);

        // Reentrega do outbox: um ciclo já cumprido recusa novo cumprimento pela própria máquina
        // de estados, então basta não insistir.
        if (expectation.CycleFor(CompetenceOf(dueDate!.Value))?.Status == CycleStatus.Fulfilled)
            return new FulfillExpectationForBillResponse(request.BillId, match.ExpectationId.Value, match.CycleId.Value);

        expectation.Fulfill(match.CycleId, bill.Id, dueDate.Value, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new FulfillExpectationForBillResponse(
            request.BillId, match.ExpectationId.Value, match.CycleId.Value);
    }

    /// <summary>
    /// O vencimento como o documento o declara. Nulo em arrecadação sem data legível — e aí não
    /// há casamento, porque dar por cumprida a conta errada é pior que alertar por uma que chegou.
    /// </summary>
    private static DateOnly? DueDateOf(Bill bill)
    {
        // Consultar o Kind antes é obrigatório: acessar a linha digitável de um instrumento Pix
        // lança BLP.INS03, por desenho.
        var due = bill.Instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Select(i => i.DigitableLine.DueDate)
            .FirstOrDefault(d => d is not null);

        return due is null ? null : DateOnly.FromDateTime(due.Value);
    }

    private static CompetencePeriod CompetenceOf(DateOnly dueDate) => new(dueDate.Year, dueDate.Month);
}
