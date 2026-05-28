namespace EconomicCore.Application.Queries.GetCashFlow;

public sealed record GetCashFlowResponse(
    string Period,
    decimal TotalOutflow);
