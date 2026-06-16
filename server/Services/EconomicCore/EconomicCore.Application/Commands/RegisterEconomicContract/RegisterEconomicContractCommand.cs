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
    PenaltyTermsModel? PenaltyTerms,
    IReadOnlyList<RegisterContractChargeModel>? Charges = null,
    string? PrimaryPurpose = null) : IRequest<RegisterEconomicContractResponse>;

/// <summary>
/// Late-payment penalty policy of the contract — mandatory on creation (CTR50 when absent). Fine and interest
/// are each PERCENT (of the commitment amount) or FIXED (amount in the contract currency); interest accrues per
/// fully elapsed DAILY/MONTHLY/YEARLY unit.
/// </summary>
public sealed record PenaltyTermsModel(
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod);

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
    PenaltyTermsModel? Penalty,
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
        Penalty,
        Charges,
        PrimaryPurpose);
}
