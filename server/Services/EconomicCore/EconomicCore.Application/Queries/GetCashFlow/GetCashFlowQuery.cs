namespace EconomicCore.Application.Queries.GetCashFlow;

using MediatR;

public sealed record GetCashFlowQuery(
    Guid TenantId,
    int Year,
    int Month) : IRequest<GetCashFlowResponse>;
