namespace EconomicCore.Application.Queries.GetCompetenceDRE;

using MediatR;

public sealed record GetCompetenceDREQuery(
    Guid TenantId,
    int Year,
    int Month) : IRequest<GetCompetenceDREResponse>;
