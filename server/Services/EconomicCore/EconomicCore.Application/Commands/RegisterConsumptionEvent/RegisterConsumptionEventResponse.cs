namespace EconomicCore.Application.Commands.RegisterConsumptionEvent;

public sealed record RegisterConsumptionEventResponse(
    Guid Id,
    string Direction,
    string ResourceKind);
