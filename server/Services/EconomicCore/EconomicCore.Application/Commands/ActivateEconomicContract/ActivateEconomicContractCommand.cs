namespace EconomicCore.Application.Commands.ActivateEconomicContract;

using MediatR;

public sealed record ActivateEconomicContractCommand(
    Guid TenantId,
    Guid ContractId) : IRequest<ActivateEconomicContractResponse>;
