namespace EconomicCore.Application.Commands.RegisterEconomicContract;

public sealed record RegisterEconomicContractResponse(
    Guid Id,
    string Status,
    string Direction,
    Guid ResourceId,
    int TermMonths,
    DateOnly StartDate);
