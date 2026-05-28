namespace EconomicCore.IntegrationTests.Contracts;

public sealed record CreateResourceRequest(string Name, string Kind);
public sealed record ResourceResponse(Guid Id, string Name, string Kind);

public sealed record CreateAgentRequest(string Name, string Scope, string? TaxIdValue, string? TaxIdKind);
public sealed record AgentResponse(Guid Id, string Name, string Scope);

public sealed record CreateContractRequest(
    Guid CounterpartyId,
    Guid ResourceId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate);

public sealed record ContractResponse(
    Guid Id,
    string Status,
    string Direction,
    Guid ResourceId,
    int TermMonths,
    DateOnly StartDate);

public sealed record GenerateCommitmentsRequest(int Year, int Month, DateTime OccurredAt);

public sealed record CommitmentDto(Guid Id, string Direction, string Status, int PeriodYear, int PeriodMonth);

public sealed record GenerateCommitmentsResponse(IReadOnlyList<CommitmentDto> Commitments);

public sealed record ActivateContractResponse(Guid Id, string Status, IReadOnlyList<CommitmentDto> Commitments);

public sealed record TerminateContractRequest(string Reason, DateOnly TerminationDate);
public sealed record TerminateContractResponse(Guid Id, string Status, int CancelledCount);

public sealed record RegisterConsumptionRequest(Guid ContractId, Guid CommitmentId, DateTime OccurredAt);
public sealed record ConsumptionEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record RegisterPaymentRequest(Guid ContractId, Guid CommitmentId, decimal Amount, string Currency, DateTime OccurredAt);
public sealed record PaymentEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record DREResponse(string Period, decimal TotalExpense);
public sealed record CashFlowResponse(string Period, decimal TotalOutflow);

public sealed record ErrorResponse(string Id, string Message);
