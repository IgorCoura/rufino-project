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
    DateOnly StartDate,
    IReadOnlyList<RegisterContractChargeModel>? Charges = null,
    string? PrimaryPurpose = null) : IRequest<RegisterEconomicContractResponse>;

/// <summary>
/// An additional recurring charge track (condominium, property tax, insurance) bundled into the contract.
/// Rent is the contract core (ExpectedAmount/Currency on the command) and is never sent as a charge.
/// </summary>
public sealed record RegisterContractChargeModel(
    string Purpose,
    decimal ExpectedAmount,
    string Currency,
    Guid ResourceId,
    Guid RecipientAgentId,
    bool CollectedByCounterparty);

public sealed record RegisterEconomicContractModel(
    Guid CounterpartyId,
    Guid ResourceId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate,
    IReadOnlyList<RegisterContractChargeModel>? Charges = null,
    string? PrimaryPurpose = null)
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
        StartDate,
        Charges,
        PrimaryPurpose);
}
