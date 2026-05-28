namespace EconomicCore.Application.Commands.TerminateEconomicContract;

public sealed record TerminateEconomicContractResponse(
    Guid Id,
    string Status,
    int CancelledCount);
