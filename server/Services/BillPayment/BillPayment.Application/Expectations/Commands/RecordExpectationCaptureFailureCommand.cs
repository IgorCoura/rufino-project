namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Um artefato chegou e travou: marca o ciclo que ele estava vindo cumprir.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o segundo dos dois alertas do ADR-014, e o mais valioso.</strong> O sistema já tem o
/// documento e sabe o que falta, então o aviso leva direto ao item resolvível — informar a senha,
/// reivindicar, digitar a linha. Até 2026-08-27 o método do agregado que registra isso não era
/// chamado por nenhum código de produção, e a lista <c>captureFailed</c> do painel voltava sempre
/// vazia.
/// </para>
/// <para>
/// <strong>A ponte é a fonte, porque não há outra.</strong> Um item preso em <c>Locked</c> ou
/// <c>LinkFailed</c> falhou antes da extração: não tem beneficiário, vencimento nem valor. Quem
/// escolhe a expectativa é o Domain Service, e ele recusa quando há mais de uma candidata — pelo
/// mesmo motivo do casamento de boleto.
/// </para>
/// </remarks>
public sealed record RecordExpectationCaptureFailureCommand(
    Guid TenantId,
    Guid CaptureItemId,
    Guid SourceId,
    string Status,
    DateTime ArrivedAt) : IRequest<RecordExpectationCaptureFailureResponse>;

public sealed record RecordExpectationCaptureFailureResponse(Guid? ExpectationId, Guid? CycleId);

public sealed class RecordExpectationCaptureFailureCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RecordExpectationCaptureFailureCommandHandler> logger)
    : IRequestHandler<RecordExpectationCaptureFailureCommand, RecordExpectationCaptureFailureResponse>
{
    public async Task<RecordExpectationCaptureFailureResponse> Handle(
        RecordExpectationCaptureFailureCommand request,
        CancellationToken cancellationToken)
    {
        var reason = ExpectationCaptureMatchingService.ReasonFor(
            Enumeration.FromDisplayName<CaptureItemStatus>(request.Status));

        // Estado que não aguarda resgate não descreve falha de captura nenhuma.
        if (reason is null)
            return new RecordExpectationCaptureFailureResponse(null, null);

        var tenantId = TenantId.From(request.TenantId);

        var candidates = await expectations.ListByHintSourceAsync(
            tenantId, CaptureSourceId.From(request.SourceId), cancellationToken);

        if (candidates.Count == 0)
            return new RecordExpectationCaptureFailureResponse(null, null);

        var now = clock.GetUtcNow().UtcDateTime;

        var match = ExpectationCaptureMatchingService.Match(
            candidates, DateOnly.FromDateTime(request.ArrivedAt), DateOnly.FromDateTime(now));

        if (match is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Artefato travado não casou com nenhum ciclo aberto entre {Count} expectativas da fonte.",
                    candidates.Count);
            }

            return new RecordExpectationCaptureFailureResponse(null, null);
        }

        var expectation = candidates.First(e => e.Id == match.ExpectationId);

        // Idempotente contra reentrega do outbox: a mesma falha do mesmo item não muda nada, e o
        // agregado sai calado em vez de emitir o aviso de novo.
        expectation.RecordCaptureFailure(
            match.CycleId, CaptureItemId.From(request.CaptureItemId), reason, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RecordExpectationCaptureFailureResponse(
            match.ExpectationId.Value, match.CycleId.Value);
    }
}

public sealed class RecordExpectationCaptureFailureIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RecordExpectationCaptureFailureIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RecordExpectationCaptureFailureCommand, RecordExpectationCaptureFailureResponse>(
        mediator, requestManager, logger)
{
    protected override RecordExpectationCaptureFailureResponse CreateResultForDuplicateRequest()
        => new(null, null);
}
