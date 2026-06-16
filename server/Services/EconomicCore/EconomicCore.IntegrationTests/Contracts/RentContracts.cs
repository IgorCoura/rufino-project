namespace EconomicCore.IntegrationTests.Contracts;

public sealed record CreateResourceRequest(string Name, string Kind);
public sealed record ResourceResponse(Guid Id, string Name, string Kind);

public sealed record CreateAgentRequest(string Name, string Scope, string? TaxIdValue, string? TaxIdKind);
public sealed record AgentResponse(Guid Id, string Name, string Scope);

public sealed record ContractChargeRequest(
    string Purpose,
    decimal ExpectedAmount,
    string Currency,
    Guid ResourceId,
    Guid RecipientAgentId,
    bool CollectedByCounterparty);

public sealed record PenaltyTermsRequest(
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod)
{
    // Política histórica do BC (multa 2% + juros 1% a.m.) — preserva a aritmética dos cenários existentes.
    public static readonly PenaltyTermsRequest Default = new("PERCENT", 0.02m, "PERCENT", 0.01m, "MONTHLY");
}

public sealed record CreateContractRequest(
    Guid CounterpartyId,
    Guid ResourceId,
    decimal ExpectedAmount,
    string Currency,
    string Direction,
    string Periodicity,
    int AnchorDay,
    int TermMonths,
    DateOnly StartDate,
    PenaltyTermsRequest Penalty,
    IReadOnlyList<ContractChargeRequest>? Charges = null,
    string? PrimaryPurpose = null);

public sealed record ChangePenaltyTermsRequest(
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod);

public sealed record ChangePenaltyTermsResponse(
    Guid Id,
    string FineKind,
    decimal FineValue,
    string InterestKind,
    decimal InterestValue,
    string InterestPeriod);

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

public sealed record AdjustContractRequest(
    string Purpose, int EffectiveFromYear, int EffectiveFromMonth, decimal? NewAmount, decimal? IndexRate, string Currency);

public sealed record TerminateContractRequest(string Reason, DateOnly TerminationDate);
public sealed record TerminateContractResponse(Guid Id, string Status, int CancelledCount);

public sealed record RegisterConsumptionRequest(Guid ContractId, Guid CommitmentId, DateTime OccurredAt);
public sealed record ConsumptionEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record RegisterPaymentRequest(Guid ContractId, Guid CommitmentId, decimal Amount, string Currency, DateTime OccurredAt);
public sealed record PaymentEventResponse(Guid Id, string Direction, string ResourceKind);

public sealed record BundledPaymentAllocationRequest(Guid CommitmentId, decimal Amount);
public sealed record RegisterBundledPaymentRequest(Guid ContractId, IReadOnlyList<BundledPaymentAllocationRequest> Allocations, string Currency, DateTime OccurredAt);
public sealed record BundledPaymentEventResponse(Guid Id, string Direction, decimal TotalAmount, int AllocationCount);

public sealed record RegisterLatePaymentRequest(Guid ContractId, Guid CommitmentId, decimal TotalAmount, string Currency, DateTime OccurredAt);
public sealed record LatePaymentEventResponse(Guid Id, decimal TotalAmount, decimal BaseAmount, decimal PenaltyAmount, Guid PenaltyCommitmentId, int AllocationCount);

public sealed record DREResponse(string Period, decimal TotalExpense);
public sealed record CashFlowResponse(string Period, decimal TotalOutflow);

public sealed record ErrorResponse(string Id, string Message);
