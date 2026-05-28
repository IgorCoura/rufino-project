namespace EconomicCore.Application.Commands.RegisterEconomicResource;

public sealed record RegisterEconomicResourceResponse(
    Guid Id,
    string Name,
    string Kind);
