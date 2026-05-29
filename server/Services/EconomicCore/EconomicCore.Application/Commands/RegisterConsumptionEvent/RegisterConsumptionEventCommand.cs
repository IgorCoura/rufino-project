namespace EconomicCore.Application.Commands.RegisterConsumptionEvent;

using EconomicCore.Application.Mediator;

public sealed record RegisterConsumptionEventCommand(
    Guid TenantId,
    Guid ContractId,
    Guid CommitmentId,
    DateTime OccurredAt,
    Guid? UserId) : IRequest<RegisterConsumptionEventResponse>;

public sealed record RegisterConsumptionEventModel(
    Guid ContractId,
    Guid CommitmentId,
    DateTime OccurredAt)
{
    public RegisterConsumptionEventCommand ToCommand(Guid tenantId, Guid? userId) => new(
        tenantId, ContractId, CommitmentId, OccurredAt, userId);
}
