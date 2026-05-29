namespace EconomicCore.Application.Queries.GetCompetenceDRE;

using EconomicCore.Application.Mediator;

public sealed record GetCompetenceDREQuery(
    Guid TenantId,
    int Year,
    int Month) : IRequest<GetCompetenceDREResponse>;
