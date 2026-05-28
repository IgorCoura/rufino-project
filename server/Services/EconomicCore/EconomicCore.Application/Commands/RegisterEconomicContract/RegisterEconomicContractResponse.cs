namespace EconomicCore.Application.Commands.RegisterEconomicContract;

public sealed record RegisterEconomicContractResponse(
    Guid Id,
    string Status,
    string Direction);
