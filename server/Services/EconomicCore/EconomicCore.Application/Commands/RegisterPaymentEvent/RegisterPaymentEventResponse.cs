namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

public sealed record RegisterPaymentEventResponse(
    Guid Id,
    string Direction,
    string ResourceKind);
