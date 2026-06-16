namespace EconomicCore.Application.Commands.ChangeContractPenaltyTerms;

using EconomicCore.Application.Mediator;

public sealed record ChangeContractPenaltyTermsCommand(
    Guid TenantId,
    Guid ContractId,
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod) : IRequest<ChangeContractPenaltyTermsResponse>;

public sealed record ChangeContractPenaltyTermsModel(
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod)
{
    public ChangeContractPenaltyTermsCommand ToCommand(Guid tenantId, Guid contractId) => new(
        tenantId,
        contractId,
        FineKind,
        FineValue,
        InterestKind,
        InterestValue,
        InterestPeriod);
}
