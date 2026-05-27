namespace EconomicCore.IntegrationTests.Contracts;

public sealed record CreateContractRequest(
    Guid CounterpartyId, decimal ExpectedAmount, string Currency,
    string Direction, string Periodicity, int AnchorDay);

public sealed record ContractResponse(Guid Id, string Status, string Direction);

public sealed record GenerateCommitmentsRequest(int Year, int Month, DateTime OccurredAt);

public sealed record CommitmentDto(Guid Id, string Direction, string Status, int PeriodYear, int PeriodMonth);

public sealed record GenerateCommitmentsResponse(IReadOnlyList<CommitmentDto> Commitments);

public sealed record RegisterConsumptionRequest(Guid ContractId, int Year, int Month, DateTime OccurredAt);

public sealed record ConsumptionEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record RegisterPaymentRequest(Guid ContractId, decimal Amount, string Currency, int Year, int Month, DateTime OccurredAt);

public sealed record PaymentEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record DREResponse(string Period, decimal TotalExpense);

public sealed record CashFlowResponse(string Period, decimal TotalOutflow);

public sealed record ErrorResponse(string Id, string Message);
