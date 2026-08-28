namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// O artefato que travava um ciclo foi resolvido: o ciclo volta a esperar.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sem isto o painel mentiria.</strong> Item reprovado, reaberto ou descartado sai da fila
/// de pendências, mas o ciclo continuaria dizendo "resolva este item" — e alerta que aponta para
/// trabalho concluído treina a pessoa a ignorar alerta tão bem quanto alerta indevido.
/// </para>
/// <para>
/// <strong>Não vale para o item que virou boleto</strong> — ali quem fecha o ciclo é o caminho de
/// cumprimento, que muda o status para <c>Fulfilled</c> e é o desfecho bom. Este comando alcança
/// só os ciclos que ainda apontam para o artefato.
/// </para>
/// </remarks>
public sealed record ClearExpectationCaptureFailureCommand(Guid TenantId, Guid CaptureItemId)
    : IRequest<ClearExpectationCaptureFailureResponse>;

public sealed record ClearExpectationCaptureFailureResponse(int ClearedCycles);

public sealed class ClearExpectationCaptureFailureCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClearExpectationCaptureFailureCommand, ClearExpectationCaptureFailureResponse>
{
    public async Task<ClearExpectationCaptureFailureResponse> Handle(
        ClearExpectationCaptureFailureCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var blocked = await expectations.ListByBlockedCaptureItemAsync(tenantId, itemId, cancellationToken);

        if (blocked.Count == 0)
            return new ClearExpectationCaptureFailureResponse(0);

        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var expectation in blocked)
            expectation.ClearCaptureFailure(itemId, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ClearExpectationCaptureFailureResponse(blocked.Count);
    }
}

public sealed class ClearExpectationCaptureFailureIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ClearExpectationCaptureFailureIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ClearExpectationCaptureFailureCommand, ClearExpectationCaptureFailureResponse>(
        mediator, requestManager, logger)
{
    protected override ClearExpectationCaptureFailureResponse CreateResultForDuplicateRequest()
        => new(0);
}
