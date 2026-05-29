namespace EconomicCore.Application.Commands.GenerateCommitments;

using EconomicCore.Application.Mediator;

public sealed record GenerateCommitmentsCommand(
    Guid TenantId,
    Guid ContractId,
    int Year,
    int Month,
    DateTime OccurredAt) : IRequest<GenerateCommitmentsResponse>;

public sealed record GenerateCommitmentsModel(int Year, int Month, DateTime OccurredAt)
{
    public GenerateCommitmentsCommand ToCommand(Guid tenantId, Guid contractId) => new(
        tenantId, contractId, Year, Month, OccurredAt);
}
