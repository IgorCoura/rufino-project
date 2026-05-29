namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

using EconomicCore.Application.Mediator;

public sealed record RegisterPaymentEventCommand(
    Guid TenantId,
    Guid ContractId,
    Guid CommitmentId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt,
    Guid? UserId) : IRequest<RegisterPaymentEventResponse>;

public sealed record RegisterPaymentEventModel(
    Guid ContractId,
    Guid CommitmentId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt)
{
    public RegisterPaymentEventCommand ToCommand(Guid tenantId, Guid? userId) => new(
        tenantId, ContractId, CommitmentId, Amount, Currency, OccurredAt, userId);
}
