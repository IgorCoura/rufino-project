namespace EconomicCore.Application.Commands.RegisterEconomicAgent;

public sealed record RegisterEconomicAgentResponse(
    Guid Id,
    string Name,
    string Scope);
