namespace EconomicCore.Application.Commands.ActivateEconomicContract;

using EconomicCore.Application.Commands.GenerateCommitments;

public sealed record ActivateEconomicContractResponse(
    Guid Id,
    string Status,
    IReadOnlyList<CommitmentDto> Commitments);
