namespace EconomicCore.Application.Commands.ChangeContractPenaltyTerms;

public sealed record ChangeContractPenaltyTermsResponse(
    Guid Id,
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod);
