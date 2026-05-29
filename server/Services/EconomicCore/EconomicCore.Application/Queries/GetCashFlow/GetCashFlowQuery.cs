namespace EconomicCore.Application.Queries.GetCashFlow;

using EconomicCore.Application.Mediator;

public sealed record GetCashFlowQuery(
    Guid TenantId,
    int Year,
    int Month) : IRequest<GetCashFlowResponse>;
