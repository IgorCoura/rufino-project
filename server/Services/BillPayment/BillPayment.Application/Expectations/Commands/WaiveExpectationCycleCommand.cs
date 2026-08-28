namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// "Este mês não vem" — dispensa um ciclo sem desativar a expectativa.
/// </summary>
/// <remarks>
/// É a defesa mais barata contra o falso positivo: um clique resolve o mês atípico, e a
/// expectativa continua valendo para os próximos. Sem ela, a única saída seria desativar a
/// expectativa inteira — e ninguém a reativaria depois.
/// </remarks>
public sealed record WaiveExpectationCycleCommand(
    Guid TenantId,
    Guid ExpectationId,
    Guid CycleId,
    Guid UserId,
    string? Reason) : ITenantScopedCommand, IRequest<WaiveExpectationCycleResponse>;

public sealed record WaiveExpectationCycleResponse(Guid Id, Guid CycleId, string Status);

public sealed class WaiveExpectationCycleCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<WaiveExpectationCycleCommand, WaiveExpectationCycleResponse>
{
    public async Task<WaiveExpectationCycleResponse> Handle(
        WaiveExpectationCycleCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var expectation = await expectations.GetAsync(
                tenantId, BillExpectationId.From(request.ExpectationId), cancellationToken)
            ?? throw BillExpectationErrors.NotFound(request.ExpectationId);

        var cycleId = ExpectationCycleId.From(request.CycleId);

        expectation.Waive(
            cycleId, UserId.From(request.UserId), request.Reason, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new WaiveExpectationCycleResponse(
            expectation.Id.Value, request.CycleId, CycleStatus.Waived.Name);
    }
}

public sealed class WaiveExpectationCycleIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<WaiveExpectationCycleIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<WaiveExpectationCycleCommand, WaiveExpectationCycleResponse>(
        mediator, requestManager, logger)
{
    protected override WaiveExpectationCycleResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, Guid.Empty, string.Empty);
}
