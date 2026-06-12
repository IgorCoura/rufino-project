namespace EconomicCore.Application.Commands.RegisterLatePaymentEvent;

using EconomicCore.Application.Mediator;

/// <summary>
/// Registers a late payment in one shot (one boleto with the updated amount = base + multa/juros): the contract
/// materializes the Penalty track synchronously and a single bundled EconomicEvent is created with two allocations
/// (base track + Penalty track). Multi-aggregate by design — the penalty is a PRE-CONDITION of the allocation
/// composition, not an effect of the event, so it cannot go through the outbox relay.
/// </summary>
public sealed record RegisterLatePaymentEventCommand(
    Guid TenantId,
    Guid ContractId,
    Guid CommitmentId,
    decimal TotalAmount,
    string Currency,
    DateTime OccurredAt,
    Guid? UserId) : IRequest<RegisterLatePaymentEventResponse>, IMultiAggregateCommand;

public sealed record RegisterLatePaymentEventModel(
    Guid ContractId,
    Guid CommitmentId,
    decimal TotalAmount,
    string Currency,
    DateTime OccurredAt)
{
    public RegisterLatePaymentEventCommand ToCommand(Guid tenantId, Guid? userId) => new(
        tenantId, ContractId, CommitmentId, TotalAmount, Currency, OccurredAt, userId);
}
