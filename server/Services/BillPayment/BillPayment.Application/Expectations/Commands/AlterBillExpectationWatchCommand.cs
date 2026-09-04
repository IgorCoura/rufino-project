namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Liga, desliga ou suspende o monitoramento de uma expectativa.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pausar e desativar são coisas diferentes, e as duas existem contra o falso positivo.</strong>
/// Pausa cobre o que tem prazo — imóvel desocupado, obra parada, férias — e volta sozinha;
/// desativação é o fim da expectativa. Alerta indevido treina o usuário a ignorar alerta, e um
/// alerta ignorado é pior que alerta nenhum, porque dá a impressão de que alguém está olhando.
/// </para>
/// <para>
/// Um comando só para os três verbos porque os três mudam o mesmo aspecto — se o monitoramento
/// vale hoje —, e separá-los faria a Application escolher entre métodos por um <c>if</c> que
/// pertence ao agregado.
/// </para>
/// </remarks>
public sealed record AlterBillExpectationWatchCommand(
    Guid TenantId,
    Guid ExpectationId,
    bool IsActive,
    DateOnly? PausedUntil,
    string? Reason) : ITenantScopedCommand, IRequest<AlterBillExpectationWatchResponse>;

public sealed record AlterBillExpectationWatchResponse(Guid Id, bool IsActive, DateOnly? PausedUntil);

public sealed class AlterBillExpectationWatchCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterBillExpectationWatchCommand, AlterBillExpectationWatchResponse>
{
    public async Task<AlterBillExpectationWatchResponse> Handle(
        AlterBillExpectationWatchCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var expectation = await expectations.GetAsync(
                tenantId, BillExpectationId.From(request.ExpectationId), cancellationToken)
            ?? throw BillExpectationErrors.NotFound(request.ExpectationId);

        var now = clock.GetUtcNow().UtcDateTime;

        if (!request.IsActive)
            expectation.Deactivate(request.Reason, now);
        else if (request.PausedUntil is { } until)
            expectation.Pause(until, now);
        else
            expectation.Reactivate(now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterBillExpectationWatchResponse(
            expectation.Id.Value, expectation.IsActive, expectation.PausedUntil);
    }
}

public sealed class AlterBillExpectationWatchIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterBillExpectationWatchIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterBillExpectationWatchCommand, AlterBillExpectationWatchResponse>(
        mediator, requestManager, logger)
{
    protected override AlterBillExpectationWatchResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, false, null);
}
