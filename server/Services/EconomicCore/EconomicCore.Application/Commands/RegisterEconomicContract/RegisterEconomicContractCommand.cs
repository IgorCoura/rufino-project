namespace EconomicCore.Application.Commands.RegisterEconomicContract;

using MediatR;

public sealed record RegisterEconomicContractCommand(
    Guid TenantId,
    Guid CounterpartyId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay) : IRequest<RegisterEconomicContractResponse>;

public sealed record RegisterEconomicContractModel(
    Guid CounterpartyId, decimal ExpectedAmount, string Currency,
    string Direction, string Periodicity, int AnchorDay)
{
    public RegisterEconomicContractCommand ToCommand(Guid tenantId) => new(
        tenantId, CounterpartyId, ExpectedAmount, Currency, Direction, Periodicity, AnchorDay);
}
