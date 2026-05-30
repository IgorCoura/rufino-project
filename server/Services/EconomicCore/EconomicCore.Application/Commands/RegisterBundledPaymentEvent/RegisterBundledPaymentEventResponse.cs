namespace EconomicCore.Application.Commands.RegisterBundledPaymentEvent;

public sealed record RegisterBundledPaymentEventResponse(
    Guid Id,
    string Direction,
    decimal TotalAmount,
    int AllocationCount);
