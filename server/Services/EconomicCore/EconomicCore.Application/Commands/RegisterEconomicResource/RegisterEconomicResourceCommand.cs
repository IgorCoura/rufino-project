namespace EconomicCore.Application.Commands.RegisterEconomicResource;

using EconomicCore.Application.Mediator;

public sealed record RegisterEconomicResourceCommand(
    Guid TenantId,
    string Name,
    string Kind) : IRequest<RegisterEconomicResourceResponse>;

public sealed record RegisterEconomicResourceModel(string Name, string Kind)
{
    public RegisterEconomicResourceCommand ToCommand(Guid tenantId) => new(tenantId, Name, Kind);
}
