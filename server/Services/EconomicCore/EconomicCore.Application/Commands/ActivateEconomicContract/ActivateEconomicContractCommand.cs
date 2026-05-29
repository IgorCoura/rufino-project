namespace EconomicCore.Application.Commands.ActivateEconomicContract;

using EconomicCore.Application.Mediator;

public sealed record ActivateEconomicContractCommand(
    Guid TenantId,
    Guid ContractId) : IRequest<ActivateEconomicContractResponse>;
