namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// One concrete approval cast against a <see cref="Payable"/> in multi-approver mode (Sprint 10).
/// Multiple records accumulate until <c>RequiredApprovalCount</c> distinct ones are present, at
/// which point <c>PayableFullyApproved</c> fires.
/// </summary>
public readonly record struct ApprovalRecord(UserId ApprovedBy, string Role, DateTime ApprovedAt);
