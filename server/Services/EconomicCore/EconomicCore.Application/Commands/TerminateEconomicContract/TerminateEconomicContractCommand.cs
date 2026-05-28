namespace EconomicCore.Application.Commands.TerminateEconomicContract;

using MediatR;

public sealed record TerminateEconomicContractCommand(
    Guid TenantId,
    Guid ContractId,
    string Reason,
    DateOnly TerminationDate) : IRequest<TerminateEconomicContractResponse>;

public sealed record TerminateEconomicContractModel(string Reason, DateOnly TerminationDate)
{
    public TerminateEconomicContractCommand ToCommand(Guid tenantId, Guid contractId) =>
        new(tenantId, contractId, Reason, TerminationDate);
}
