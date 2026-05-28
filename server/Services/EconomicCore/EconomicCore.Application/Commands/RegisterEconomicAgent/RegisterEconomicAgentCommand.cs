namespace EconomicCore.Application.Commands.RegisterEconomicAgent;

using MediatR;

public sealed record RegisterEconomicAgentCommand(
    Guid TenantId,
    string Name,
    string Scope,
    string? TaxIdValue,
    string? TaxIdKind) : IRequest<RegisterEconomicAgentResponse>;

public sealed record RegisterEconomicAgentModel(
    string Name,
    string Scope,
    string? TaxIdValue,
    string? TaxIdKind)
{
    public RegisterEconomicAgentCommand ToCommand(Guid tenantId) => new(
        tenantId, Name, Scope, TaxIdValue, TaxIdKind);
}
