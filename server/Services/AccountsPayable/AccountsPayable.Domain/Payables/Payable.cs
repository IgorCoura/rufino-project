namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.Events;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Entities;

/// <summary>
/// Aggregate Root of the Accounts Payable BC. Event-Sourced (decision D-405): mutations only inside
/// <c>When(SpecificEvent)</c> handlers; public command methods validate and call <see cref="EventSourcedAggregateRoot{TId}.Apply"/>.
/// <para>
/// State machine after Sprint 6 (PaymentOrder hooks):
/// <c>Draft → Scheduled | Paid | Cancelled | AwaitingApproval</c>;
/// <c>Scheduled → Paid | Cancelled | PaymentRequested</c>;
/// <c>AwaitingApproval → Approved | Rejected | Cancelled</c>;
/// <c>Approved → Scheduled | Paid | Cancelled</c>;
/// <c>PaymentRequested → Paid | PaymentFailed</c>;
/// <c>PaymentFailed → PaymentRequested (retry) | Cancelled</c>;
/// <c>Paid</c>, <c>Cancelled</c> and <c>Rejected</c> are terminal.
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
    public PaymentMethod? PaymentMethod { get; private set; }
    public SupplierBankAccountId? PaymentBankAccountId { get; private set; }
    public PaymentOrderId? LastPaymentOrderId { get; private set; }
    public DateTime? PaymentRequestedAt { get; private set; }
    public DateTime? PaymentFailedAt { get; private set; }
    public string? PaymentFailureReason { get; private set; }
    public CapturedBillId? CapturedBillId { get; private set; }
    public InstallmentPlanId? InstallmentPlanId { get; private set; }
    public int? InstallmentNumber { get; private set; }
    public ExpenseClassificationRuleId? LastClassificationRuleId { get; private set; }

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

    /// <summary>
    /// Factory used by the Application handler that consumes <c>CapturedBillApproved</c> from
    /// the sibling <c>BillIngestion</c> BC. Identical contract to <see cref="Initialize"/> except
    /// the payable is linked to the originating <paramref name="capturedBillId"/>.
    /// <para>
    /// <b>Dedup is enforced outside the Aggregate</b>: a unique index on
    /// <c>(TenantId, CapturedBillId)</c> at the Infra layer prevents two Payables from being
    /// created for the same capture when the Application handler re-receives the integration
    /// event. The Aggregate only guarantees that the link is captured in the event stream.
    /// </para>
    /// </summary>
    public static Payable InitializeFromCapture(
        PayableId id,
        TenantId tenantId,
        CapturedBillId capturedBillId,
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
        instance.Apply(new PayableCreatedFromCapture(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: id,
            TenantId: tenantId,
            CapturedBillId: capturedBillId,
            SupplierId: supplierId,
            AmountValue: amount.Amount,
            AmountCurrency: amount.Currency.Name,
            DueDate: dueDate.Value,
            Description: description.Value));

        return instance;
    }

    /// <summary>
    /// Factory used by <c>InstallmentPlanFactory</c> when producing the chain of <see cref="Payable"/>s
    /// that compose a parcelamento. Identical contract to <see cref="Initialize"/> except the payable
    /// is linked to its parent plan and carries the 1-based <paramref name="installmentNumber"/>.
    /// </summary>
    public static Payable InitializeAsInstallment(
        PayableId id,
        TenantId tenantId,
        InstallmentPlanId installmentPlanId,
        int installmentNumber,
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
        if (installmentNumber < 1)
            throw PayableErrors.InstallmentNumberMustBePositive(installmentNumber);

        var instance = new Payable();
        instance.Apply(new PayableCreatedAsInstallment(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: id,
            TenantId: tenantId,
            InstallmentPlanId: installmentPlanId,
            InstallmentNumber: installmentNumber,
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
    /// payables whose <see cref="Amount"/> exceeds it cannot be scheduled unless approval has
    /// already been granted (<see cref="ApprovedAt"/> is set).
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

    /// <summary>
    /// Apply a classification decided by the <c>PayableAutoClassifier</c> Domain Service (Sprint 9).
    /// Same lifecycle restrictions as <see cref="Classify"/> — terminal payables reject the call.
    /// Differs in that <see cref="ClassifiedBy"/> stays null (no human author); the audit trail
    /// flows through <see cref="LastClassificationRuleId"/> instead.
    /// </summary>
    public void ClassifyAutomatically(
        AccountId accountId,
        CostCenterId costCenterId,
        ExpenseClassificationRuleId ruleId,
        DateTime occurredAt)
    {
        if (Status == PayableStatus.Paid || Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotClassifyTerminalPayable(Status.Name);

        Apply(new PayableAutoClassified(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            AccountId: accountId,
            CostCenterId: costCenterId,
            RuleId: ruleId));
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

    /// <summary>
    /// Approval is considered already granted once <see cref="ApprovedAt"/> is set. This keeps the
    /// rule consistent across the post-approval lifecycle: <c>Approved → Scheduled → PaymentRequested
    /// → PaymentFailed → PaymentRequested</c> — the threshold check would otherwise re-trigger after
    /// the status leaves <see cref="PayableStatus.Approved"/>.
    /// </summary>
    private bool RequiresApproval(Money? threshold)
    {
        if (threshold is null) return false;
        if (ApprovedAt is not null) return false;
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

    /// <summary>
    /// Hand the payable off to <c>PaymentExecution</c> by recording the chosen channel and bank
    /// account. Emits <see cref="PayablePaymentRequested"/> — the Application layer is responsible
    /// for turning it into a <c>PaymentOrder</c> on the sibling BC. Allowed from <see cref="PayableStatus.Scheduled"/>
    /// (happy path) and from <see cref="PayableStatus.PaymentFailed"/> (retry after the bank rejected
    /// the previous order).
    /// </summary>
    public void RequestPayment(
        PaymentMethod method,
        SupplierBankAccountId bankAccountId,
        DateTime occurredAt,
        Money? approvalThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!Status.CanTransitionTo(PayableStatus.PaymentRequested))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.PaymentRequested.Name);

        if (RequiresApproval(approvalThreshold))
            throw PayableErrors.RequiresApproval(Amount.Amount, approvalThreshold!.Amount);

        Apply(new PayablePaymentRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            SupplierId: SupplierId,
            AmountValue: Amount.Amount,
            AmountCurrency: Amount.Currency.Name,
            BankAccountId: bankAccountId,
            Method: method.Name));
    }

    /// <summary>
    /// Confirm that the <c>PaymentOrder</c> sibling BC successfully settled the payment. Idempotent:
    /// receiving the same <paramref name="paymentOrderId"/> a second time on a Paid payable is a no-op
    /// (no new event emitted) — the integration event bus may redeliver. A different
    /// <paramref name="paymentOrderId"/> after the first confirmation is rejected with AP.PAY11
    /// (defends against mismatched callbacks).
    /// </summary>
    public void ConfirmPaid(PaymentOrderId paymentOrderId, DateTime paidAt, DateTime occurredAt)
    {
        if (Status == PayableStatus.Paid)
        {
            if (LastPaymentOrderId is { } current && current == paymentOrderId)
                return;
            throw PayableErrors.PaymentOrderIdMismatch(
                expected: LastPaymentOrderId?.Value ?? Guid.Empty,
                received: paymentOrderId.Value);
        }

        // PaymentRequested é a única origem válida — Draft/Scheduled chegam em Paid via MarkAsPaidManually,
        // não via callback de PaymentOrder. Sem esta guarda, eventos órfãos vindos de PaymentExecution
        // poderiam marcar como pago um Payable que nunca emitiu PayablePaymentRequested.
        if (Status != PayableStatus.PaymentRequested)
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Paid.Name);

        Apply(new PayablePaid(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            PaymentOrderId: paymentOrderId,
            PaidAt: paidAt));
    }

    /// <summary>
    /// Record that <c>PaymentExecution</c> reported a failure for the last requested payment.
    /// Status moves to <see cref="PayableStatus.PaymentFailed"/>, which is non-terminal — the
    /// caller can issue a new <see cref="RequestPayment"/> (retry) or <see cref="Cancel"/>.
    /// </summary>
    public void MarkPaymentFailed(PaymentOrderId paymentOrderId, string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw PayableErrors.PaymentFailureReasonRequired();

        if (!Status.CanTransitionTo(PayableStatus.PaymentFailed))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.PaymentFailed.Name);

        Apply(new PayablePaymentFailed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            PaymentOrderId: paymentOrderId,
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

    private void When(PayableCreatedFromCapture e)
    {
        Id = e.PayableId;
        TenantId = e.TenantId;
        SupplierId = e.SupplierId;
        Amount = new Money(e.AmountValue, Enumeration.FromDisplayName<Currency>(e.AmountCurrency));
        DueDate = new DueDate(e.DueDate);
        Description = new Description(e.Description);
        Status = PayableStatus.Draft;
        CapturedBillId = e.CapturedBillId;
    }

    private void When(PayableCreatedAsInstallment e)
    {
        Id = e.PayableId;
        TenantId = e.TenantId;
        SupplierId = e.SupplierId;
        Amount = new Money(e.AmountValue, Enumeration.FromDisplayName<Currency>(e.AmountCurrency));
        DueDate = new DueDate(e.DueDate);
        Description = new Description(e.Description);
        Status = PayableStatus.Draft;
        InstallmentPlanId = e.InstallmentPlanId;
        InstallmentNumber = e.InstallmentNumber;
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
        LastClassificationRuleId = null; // manual classification clears any prior rule attribution
    }

    private void When(PayableAutoClassified e)
    {
        AccountId = e.AccountId;
        CostCenterId = e.CostCenterId;
        ClassifiedBy = null; // automatic — audit trail is RuleId
        ClassifiedAt = e.OccurredAt;
        LastClassificationRuleId = e.RuleId;
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

    private void When(PayablePaymentRequested e)
    {
        Status = PayableStatus.PaymentRequested;
        PaymentMethod = Enumeration.FromDisplayName<PaymentMethod>(e.Method);
        PaymentBankAccountId = e.BankAccountId;
        PaymentRequestedAt = e.OccurredAt;
        PaymentFailureReason = null;
        PaymentFailedAt = null;
    }

    private void When(PayablePaid e)
    {
        Status = PayableStatus.Paid;
        PaidAt = e.PaidAt;
        LastPaymentOrderId = e.PaymentOrderId;
    }

    private void When(PayablePaymentFailed e)
    {
        Status = PayableStatus.PaymentFailed;
        LastPaymentOrderId = e.PaymentOrderId;
        PaymentFailedAt = e.OccurredAt;
        PaymentFailureReason = e.Reason;
    }
}
