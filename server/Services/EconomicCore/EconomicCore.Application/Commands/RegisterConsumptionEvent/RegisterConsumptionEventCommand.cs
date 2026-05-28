namespace EconomicCore.Application.Commands.RegisterConsumptionEvent;

using MediatR;

public sealed record RegisterConsumptionEventCommand(
    Guid TenantId,
    Guid ContractId,
    int Year,
    int Month,
    DateTime OccurredAt,
    Guid? UserId) : IRequest<RegisterConsumptionEventResponse>;

public sealed record RegisterConsumptionEventModel(Guid ContractId, int Year, int Month, DateTime OccurredAt)
{
    public RegisterConsumptionEventCommand ToCommand(Guid tenantId, Guid? userId) => new(
        tenantId, ContractId, Year, Month, OccurredAt, userId);
}
