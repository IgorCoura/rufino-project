namespace EconomicCore.Application.Commands.RegisterLatePaymentEvent;

public sealed record RegisterLatePaymentEventResponse(
    Guid Id,
    decimal TotalAmount,
    decimal BaseAmount,
    decimal PenaltyAmount,
    Guid PenaltyCommitmentId,
    int AllocationCount);
