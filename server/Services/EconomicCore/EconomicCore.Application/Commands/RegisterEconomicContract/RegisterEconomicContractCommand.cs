namespace EconomicCore.Application.Commands.RegisterEconomicContract;

using EconomicCore.Application.Mediator;

public sealed record RegisterEconomicContractCommand(
    Guid TenantId,
    Guid CounterpartyId,
    Guid ResourceId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate) : IRequest<RegisterEconomicContractResponse>;

public sealed record RegisterEconomicContractModel(
    Guid CounterpartyId,
    Guid ResourceId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate)
{
    public RegisterEconomicContractCommand ToCommand(Guid tenantId) => new(
        tenantId,
        CounterpartyId,
        ResourceId,
        ExpectedAmount,
        Currency,
        Direction,
        Periodicity,
        AnchorDay,
        TermMonths,
        StartDate);
}
