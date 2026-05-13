namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.Events;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Aggregate Root of the Accounts Payable BC. Event-Sourced (decision D-405): mutations only inside
/// <c>When(SpecificEvent)</c> handlers; public command methods validate and call <see cref="EventSourcedAggregateRoot{TId}.Apply"/>.
/// <para>
/// State machine: <c>Draft → Scheduled | Paid | Cancelled</c>; <c>Scheduled → Paid | Cancelled</c>;
/// <c>Paid</c> and <c>Cancelled</c> are terminal.
/// </para>
/// </summary>
public sealed class Payable : EventSourcedAggregateRoot<PayableId>
{
    public TenantId TenantId { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DueDate DueDate { get; private set; } = default!;
    public Description Description { get; private set; } = default!;
    public PayableStatus Status { get; private set; } = default!;
    public DateOnly? ScheduledFor { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public PaymentProof? PaymentProof { get; private set; }
    public AccountId? AccountId { get; private set; }
    public CostCenterId? CostCenterId { get; private set; }
    public DateTime? ClassifiedAt { get; private set; }
    public UserId? ClassifiedBy { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public UserId? RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private Payable() : base() { }

    private Payable(IEnumerable<IDomainEvent> history) : base(history) { }

    public static Payable Initialize(
        PayableId id,
        TenantId tenantId,
        SupplierId supplierId,
        Money amount,
        DueDate dueDate,
        Description description,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(dueDate);
        ArgumentNullException.ThrowIfNull(description);

        var today = DateOnly.FromDateTime(occurredAt);
        if (dueDate.Value < today)
            throw PayableErrors.DueDateInPast(dueDate.Value, today);

        var instance = new Payable();
        instance.Apply(new PayableCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: id,
            TenantId: tenantId,
            SupplierId: supplierId,
            AmountValue: amount.Amount,
            AmountCurrency: amount.Currency.Name,
            DueDate: dueDate.Value,
            Description: description.Value));

        return instance;
    }

    public static Payable Rehydrate(IEnumerable<IDomainEvent> history) => new(history);

    /// <summary>
    /// Schedule the payable for a future payment date.
    /// <para>
    /// <paramref name="allowUnclassified"/> (Sprint 4) bypasses the classification requirement.
    /// <paramref name="approvalThreshold"/> (Sprint 5) is the tenant's approval threshold — when set,
    /// payables whose <see cref="Amount"/> exceeds it cannot be scheduled unless <see cref="Status"/>
    /// is already <see cref="PayableStatus.Approved"/>.
    /// </para>
    /// </summary>
    public void Schedule(
        DateOnly scheduledFor,
        DateTime occurredAt,
        bool allowUnclassified = false,
        Money? approvalThreshold = null)
    {
        if (!Status.CanTransitionTo(PayableStatus.Scheduled))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Scheduled.Name);

        if (!allowUnclassified && AccountId is null)
            throw PayableErrors.CannotScheduleWithoutClassification();

        if (RequiresApproval(approvalThreshold))
            throw PayableErrors.RequiresApproval(Amount.Amount, approvalThreshold!.Amount);

        Apply(new PayableScheduled(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ScheduledFor: scheduledFor));
    }

    /// <summary>
    /// Classify the payable against an accounting account (within the tenant's chart of accounts)
    /// and a cost center. Allowed on Draft and Scheduled; reclassification is allowed and emits a
    /// new <see cref="PayableClassified"/> event each time (A+ES preserves the history).
    /// </summary>
    public void Classify(AccountId accountId, CostCenterId costCenterId, UserId classifiedBy, DateTime occurredAt)
    {
        if (Status == PayableStatus.Paid || Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotClassifyTerminalPayable(Status.Name);

        Apply(new PayableClassified(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            AccountId: accountId,
            CostCenterId: costCenterId,
            ClassifiedBy: classifiedBy));
    }

    public void MarkAsPaidManually(
        PaymentProof proof,
        DateTime paidAt,
        DateTime occurredAt,
        Money? approvalThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(proof);

        if (Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotPayCancelled();
        if (!Status.CanTransitionTo(PayableStatus.Paid))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Paid.Name);

        if (RequiresApproval(approvalThreshold))
            throw PayableErrors.RequiresApproval(Amount.Amount, approvalThreshold!.Amount);

        Apply(new PayableMarkedAsPaid(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            PaidAt: paidAt,
            ProofUri: proof.Uri,
            ProofType: proof.Type.Name));
    }

    private bool RequiresApproval(Money? threshold)
    {
        if (threshold is null) return false;
        if (Status == PayableStatus.Approved) return false;
        return Amount.Amount > threshold.Amount;
    }

    /// <summary>
    /// Move a classified Draft to <see cref="PayableStatus.AwaitingApproval"/>, starting the
    /// single-approver flow. The actual authorization of who-can-approve lives in Application/Keycloak.
    /// </summary>
    public void RequestApproval(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.AwaitingApproval))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.AwaitingApproval.Name);
        if (AccountId is null)
            throw PayableErrors.RequiresClassificationBeforeApproval();

        Apply(new PayableApprovalRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id));
    }

    public void Approve(UserId approver, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.Approved))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Approved.Name);

        Apply(new PayableApproved(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ApprovedBy: approver));
    }

    public void Reject(UserId approver, string reason, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.Rejected))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Rejected.Name);
        if (string.IsNullOrWhiteSpace(reason))
            throw PayableErrors.RejectionReasonRequired();

        Apply(new PayableRejected(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            RejectedBy: approver,
            Reason: reason.Trim()));
    }

    public void Cancel(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw PayableErrors.ReasonRequired();
        if (!Status.CanTransitionTo(PayableStatus.Cancelled))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Cancelled.Name);

        Apply(new PayableCancelled(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            Reason: reason.Trim()));
    }

    // ---- When handlers (state mutation — único lugar permitido) ----

    private void When(PayableCreated e)
    {
        Id = e.PayableId;
        TenantId = e.TenantId;
        SupplierId = e.SupplierId;
        Amount = new Money(e.AmountValue, Enumeration.FromDisplayName<Currency>(e.AmountCurrency));
        DueDate = new DueDate(e.DueDate);
        Description = new Description(e.Description);
        Status = PayableStatus.Draft;
    }

    private void When(PayableScheduled e)
    {
        ScheduledFor = e.ScheduledFor;
        Status = PayableStatus.Scheduled;
    }

    private void When(PayableMarkedAsPaid e)
    {
        PaidAt = e.PaidAt;
        PaymentProof = new PaymentProof(e.ProofUri, Enumeration.FromDisplayName<PaymentProofType>(e.ProofType));
        Status = PayableStatus.Paid;
    }

    private void When(PayableCancelled e)
    {
        Status = PayableStatus.Cancelled;
    }

    private void When(PayableClassified e)
    {
        AccountId = e.AccountId;
        CostCenterId = e.CostCenterId;
        ClassifiedBy = e.ClassifiedBy;
        ClassifiedAt = e.OccurredAt;
    }

    private void When(PayableApprovalRequested e)
    {
        Status = PayableStatus.AwaitingApproval;
    }

    private void When(PayableApproved e)
    {
        Status = PayableStatus.Approved;
        ApprovedBy = e.ApprovedBy;
        ApprovedAt = e.OccurredAt;
    }

    private void When(PayableRejected e)
    {
        Status = PayableStatus.Rejected;
        RejectedBy = e.RejectedBy;
        RejectedAt = e.OccurredAt;
        RejectionReason = e.Reason;
    }
}
