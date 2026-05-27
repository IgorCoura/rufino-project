namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

using MediatR;

public sealed record RegisterPaymentEventCommand(
    Guid TenantId,
    Guid ContractId,
    decimal Amount,
    string Currency,
    int Year,
    int Month,
    DateTime OccurredAt) : IRequest<RegisterPaymentEventResponse>;

public sealed record RegisterPaymentEventModel(
    Guid ContractId, decimal Amount, string Currency, int Year, int Month, DateTime OccurredAt)
{
    public RegisterPaymentEventCommand ToCommand(Guid tenantId) => new(
        tenantId, ContractId, Amount, Currency, Year, Month, OccurredAt);
}
