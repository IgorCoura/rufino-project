namespace EconomicCore.Application.Commands.GenerateCommitments;

public sealed record GenerateCommitmentsResponse(
    IReadOnlyList<CommitmentDto> Commitments);

public sealed record CommitmentDto(
    Guid Id,
    string Direction,
    string Status,
    int PeriodYear,
    int PeriodMonth);
