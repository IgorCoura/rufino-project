namespace EconomicCore.Application.Commands.RegisterBundledPaymentEvent;

using EconomicCore.Application.Mediator;

/// <summary>
/// Registers a single cash payment (one boleto) that settles several commitments of the same contract at once
/// (rent + condominium + property tax). One EconomicEvent is created with one allocation per line; each leg's
/// duality closes independently against its reciprocal consumption (deferred, via the outbox).
/// </summary>
public sealed record RegisterBundledPaymentEventCommand(
    Guid TenantId,
    Guid ContractId,
    IReadOnlyList<BundledPaymentAllocationModel> Allocations,
    string Currency,
    DateTime OccurredAt,
    Guid? UserId) : IRequest<RegisterBundledPaymentEventResponse>;

public sealed record BundledPaymentAllocationModel(Guid CommitmentId, decimal Amount);

public sealed record RegisterBundledPaymentEventModel(
    Guid ContractId,
    IReadOnlyList<BundledPaymentAllocationModel> Allocations,
    string Currency,
    DateTime OccurredAt)
{
    public RegisterBundledPaymentEventCommand ToCommand(Guid tenantId, Guid? userId) => new(
        tenantId, ContractId, Allocations, Currency, OccurredAt, userId);
}
