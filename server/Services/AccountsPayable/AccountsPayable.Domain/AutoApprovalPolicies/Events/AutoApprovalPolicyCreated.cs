namespace AccountsPayable.Domain.AutoApprovalPolicies.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record AutoApprovalPolicyCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    AutoApprovalPolicyId PolicyId) : IDomainEvent;
