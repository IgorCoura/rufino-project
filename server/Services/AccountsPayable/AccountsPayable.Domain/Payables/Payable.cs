namespace AccountsPayable.Domain.Payables;

using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.Events;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

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
    public AccountRef? Classification { get; private set; }
    public CostCenterId? CostCenterId { get; private set; }
    public DateTime? ClassifiedAt { get; private set; }
    public UserId? ClassifiedBy { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public UserId? RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public PaymentInstrument PaymentInstrument { get; private set; } = default!;
    public PaymentOrderId? LastPaymentOrderId { get; private set; }
    public DateTime? PaymentRequestedAt { get; private set; }
    public DateTime? PaymentFailedAt { get; private set; }
    public string? PaymentFailureReason { get; private set; }
    public CapturedBillId? CapturedBillId { get; private set; }
    public InstallmentPlanId? InstallmentPlanId { get; private set; }
    public int? InstallmentNumber { get; private set; }
    public ExpenseClassificationRuleId? LastClassificationRuleId { get; private set; }
    public int RequiredApprovalCount { get; private set; }
    public IReadOnlyList<string> EligibleApproverRoles { get; private set; } = Array.Empty<string>();
    public bool IsInstrumentOutdated { get; private set; }
    public DateTime? OutdatedAt { get; private set; }
    public string? OutdatedReason { get; private set; }
    private readonly List<ApprovalRecord> _approvalsReceived = [];
    public IReadOnlyList<ApprovalRecord> ApprovalsReceived => _approvalsReceived.AsReadOnly();
    public bool IsMultiApproval => RequiredApprovalCount > 0;

    private Payable() : base() { }

    private Payable(IEnumerable<IDomainEvent> history) : base(history) { }

    public static Payable Initialize(
        PayableId id,
        TenantId tenantId,
        SupplierId supplierId,
        Money amount,
        DueDate dueDate,
        Description description,
        PaymentInstrument paymentInstrument,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(dueDate);
        ArgumentNullException.ThrowIfNull(description);
        if (paymentInstrument is null)
            throw PayableErrors.PaymentInstrumentRequired();

        var today = DateOnly.FromDateTime(occurredAt);
        if (dueDate.Value < today)
            throw PayableErrors.DueDateInPast(dueDate.Value, today);

        var (kind, legalName, taxIdValue, taxIdType, pixVal, pixType,
             bankCode, branch, accNumber, accType,
             emvPayload, barcodeDigits) = PaymentInstrumentSerialization.Expand(paymentInstrument);

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
            Description: description.Value,
            InstrumentKind: kind,
            SupplierLegalName: legalName,
            SupplierTaxIdValue: taxIdValue,
            SupplierTaxIdType: taxIdType,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType,
            EmvPayload: emvPayload,
            BarcodeDigits: barcodeDigits));

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
        PaymentInstrument paymentInstrument,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(dueDate);
        ArgumentNullException.ThrowIfNull(description);
        if (paymentInstrument is null)
            throw PayableErrors.PaymentInstrumentRequired();

        var today = DateOnly.FromDateTime(occurredAt);
        if (dueDate.Value < today)
            throw PayableErrors.DueDateInPast(dueDate.Value, today);

        var (kind, legalName, taxIdValue, taxIdType, pixVal, pixType,
             bankCode, branch, accNumber, accType,
             emvPayload, barcodeDigits) = PaymentInstrumentSerialization.Expand(paymentInstrument);

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
            Description: description.Value,
            InstrumentKind: kind,
            SupplierLegalName: legalName,
            SupplierTaxIdValue: taxIdValue,
            SupplierTaxIdType: taxIdType,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType,
            EmvPayload: emvPayload,
            BarcodeDigits: barcodeDigits));

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
        PaymentInstrument paymentInstrument,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentNullException.ThrowIfNull(dueDate);
        ArgumentNullException.ThrowIfNull(description);
        if (paymentInstrument is null)
            throw PayableErrors.PaymentInstrumentRequired();

        var today = DateOnly.FromDateTime(occurredAt);
        if (dueDate.Value < today)
            throw PayableErrors.DueDateInPast(dueDate.Value, today);
        if (installmentNumber < 1)
            throw PayableErrors.InstallmentNumberMustBePositive(installmentNumber);

        var (kind, legalName, taxIdValue, taxIdType, pixVal, pixType,
             bankCode, branch, accNumber, accType,
             emvPayload, barcodeDigits) = PaymentInstrumentSerialization.Expand(paymentInstrument);

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
            Description: description.Value,
            InstrumentKind: kind,
            SupplierLegalName: legalName,
            SupplierTaxIdValue: taxIdValue,
            SupplierTaxIdType: taxIdType,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType,
            EmvPayload: emvPayload,
            BarcodeDigits: barcodeDigits));

        return instance;
    }

    public static Payable Rehydrate(IEnumerable<IDomainEvent> history) => new(history);

    /// <summary>
    /// Schedule the payable for a future payment date.
    /// <para>
    /// <paramref name="allowUnclassified"/> (Sprint 4) bypasses the classification requirement.
    /// </para>
    /// <para>
    /// Approval gating is enforced by the state machine: when the Application orchestrator decides
    /// an approval is required (via <c>ApprovalRequirementCalculator</c> reading the tenant's
    /// <c>AutoApprovalPolicy</c>), it calls <see cref="RequestApproval"/> / <see cref="RequestMultiApproval"/>
    /// before <see cref="Schedule"/>. The resulting <see cref="PayableStatus.AwaitingApproval"/>
    /// blocks <see cref="Schedule"/> via <c>CanTransitionTo</c> until <see cref="Approve"/> /
    /// <see cref="RecordApproval"/> moves the payable to <see cref="PayableStatus.Approved"/>.
    /// </para>
    /// </summary>
    public void Schedule(
        DateOnly scheduledFor,
        DateTime occurredAt,
        bool allowUnclassified = false)
    {
        if (!Status.CanTransitionTo(PayableStatus.Scheduled))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Scheduled.Name);

        if (!allowUnclassified && Classification is null)
            throw PayableErrors.CannotScheduleWithoutClassification();

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
    /// <para>
    /// The account reference must be anchored to its owning <c>ChartOfAccounts</c> via
    /// <see cref="AccountRef"/> — bare <c>AccountId</c> would leak an internal Entity Id from another
    /// Aggregate. Cross-aggregate consistency (chart exists, account is active and of type Expense,
    /// cost center belongs to the same tenant) is enforced by <c>PayableClassificationValidator</c>
    /// before the call.
    /// </para>
    /// </summary>
    public void Classify(AccountRef accountRef, CostCenterId costCenterId, UserId classifiedBy, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(accountRef);

        if (Status == PayableStatus.Paid || Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotClassifyTerminalPayable(Status.Name);

        Apply(new PayableClassified(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ChartOfAccountsId: accountRef.ChartOfAccountsId,
            AccountId: accountRef.AccountId,
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
        AccountRef accountRef,
        CostCenterId costCenterId,
        ExpenseClassificationRuleId ruleId,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(accountRef);

        if (Status == PayableStatus.Paid || Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotClassifyTerminalPayable(Status.Name);

        Apply(new PayableAutoClassified(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ChartOfAccountsId: accountRef.ChartOfAccountsId,
            AccountId: accountRef.AccountId,
            CostCenterId: costCenterId,
            RuleId: ruleId));
    }

    /// <summary>
    /// Manually mark the payable as paid (cliente bateu PIX/boleto pelo banco antigo e está só
    /// registrando o fato). Same approval gating as <see cref="Schedule"/>: if approval is required,
    /// the orchestrator must drive the payable through <see cref="PayableStatus.AwaitingApproval"/>
    /// → <see cref="PayableStatus.Approved"/> first — <c>CanTransitionTo</c> blocks the bypass.
    /// </summary>
    public void MarkAsPaidManually(
        PaymentProof proof,
        DateTime paidAt,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(proof);

        if (Status == PayableStatus.Cancelled)
            throw PayableErrors.CannotPayCancelled();
        if (!Status.CanTransitionTo(PayableStatus.Paid))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Paid.Name);

        Apply(new PayableMarkedAsPaid(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            PaidAt: paidAt,
            ProofUri: proof.Uri,
            ProofType: proof.Type.Name));
    }

    /// <summary>
    /// Move a classified Draft to <see cref="PayableStatus.AwaitingApproval"/>, starting the
    /// single-approver flow. The actual authorization of who-can-approve lives in Application/Keycloak.
    /// </summary>
    public void RequestApproval(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.AwaitingApproval))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.AwaitingApproval.Name);
        if (Classification is null)
            throw PayableErrors.RequiresClassificationBeforeApproval();

        Apply(new PayableApprovalRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id));
    }

    public void Approve(UserId approver, DateTime occurredAt)
    {
        if (IsMultiApproval)
            throw PayableErrors.SingleApprovalNotAllowedInMultiMode();
        if (!Status.CanTransitionTo(PayableStatus.Approved))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Approved.Name);

        Apply(new PayableApproved(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ApprovedBy: approver));
    }

    /// <summary>
    /// Multi-approver variant (Sprint 10). Replaces <see cref="RequestApproval"/> when the
    /// <c>ApprovalRequirementCalculator</c> says more than one approval is needed. Sets up the
    /// requirement state (count + eligible roles) and moves to <see cref="PayableStatus.AwaitingApproval"/>.
    /// Subsequent approvals use <see cref="RecordApproval"/>, not <see cref="Approve"/>.
    /// </summary>
    public void RequestMultiApproval(
        int requiredCount,
        IReadOnlyList<string> eligibleRoles,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(eligibleRoles);
        if (requiredCount < 1)
            throw PayableErrors.MultiApprovalRequiredCountTooLow(requiredCount);
        if (eligibleRoles.Count == 0)
            throw PayableErrors.MultiApprovalEligibleRolesRequired();
        if (!Status.CanTransitionTo(PayableStatus.AwaitingApproval))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.AwaitingApproval.Name);
        if (Classification is null)
            throw PayableErrors.RequiresClassificationBeforeApproval();

        Apply(new PayableMultiApprovalRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            RequiredApprovalCount: requiredCount,
            EligibleApproverRoles: eligibleRoles));
    }

    /// <summary>
    /// Record one approval in multi-approver mode (Sprint 10). The approver must have a role from
    /// <see cref="EligibleApproverRoles"/> and must not have approved before. Once the count of
    /// recorded approvals reaches <see cref="RequiredApprovalCount"/>, emits <c>PayableFullyApproved</c>
    /// in the same call — both events land in <see cref="EventSourcedAggregateRoot{TId}.Changes"/>.
    /// </summary>
    public void RecordApproval(UserId approver, string role, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw PayableErrors.ApproverRoleNotEligible(role ?? string.Empty);
        if (!IsMultiApproval)
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Approved.Name);
        if (Status != PayableStatus.AwaitingApproval)
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Approved.Name);

        var normalizedRole = role.Trim().ToUpperInvariant();
        if (!EligibleApproverRoles.Contains(normalizedRole))
            throw PayableErrors.ApproverRoleNotEligible(normalizedRole);
        if (_approvalsReceived.Any(a => a.ApprovedBy == approver))
            throw PayableErrors.ApproverAlreadyRecorded(approver.Value);

        Apply(new PayableApprovalRecorded(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ApprovedBy: approver,
            Role: normalizedRole));

        if (_approvalsReceived.Count >= RequiredApprovalCount)
        {
            Apply(new PayableFullyApproved(
                EventId: Guid.NewGuid(),
                OccurredAt: occurredAt,
                PayableId: Id));
        }
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
    /// Hand the payable off to <c>PaymentExecution</c>. O <c>PaymentMethod</c> e o
    /// <c>PaymentInstrument</c> já estão no estado desde a criação (Sprint 12.B); o evento
    /// serializa o instrumento via <c>PaymentInstrumentSerialization</c> para o PSP processar.
    /// Permitido de <see cref="PayableStatus.Scheduled"/> (happy path) e de
    /// <see cref="PayableStatus.PaymentFailed"/> (retry).
    /// </summary>
    public void RequestPayment(DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.PaymentRequested))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.PaymentRequested.Name);

        var (kind, legalName, taxIdValue, taxIdType, pixVal, pixType,
             bankCode, branch, accNumber, accType,
             emvPayload, barcodeDigits) = PaymentInstrumentSerialization.Expand(PaymentInstrument);

        Apply(new PayablePaymentRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            SupplierId: SupplierId,
            AmountValue: Amount.Amount,
            AmountCurrency: Amount.Currency.Name,
            InstrumentKind: kind,
            SupplierLegalName: legalName,
            SupplierTaxIdValue: taxIdValue,
            SupplierTaxIdType: taxIdType,
            PixKeyValue: pixVal,
            PixKeyType: pixType,
            BankCode: bankCode,
            Branch: branch,
            AccountNumber: accNumber,
            AccountType: accType,
            EmvPayload: emvPayload,
            BarcodeDigits: barcodeDigits));
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
    /// Marca este Payable como tendo um <see cref="PaymentInstrument"/> desatualizado em relação
    /// ao estado atual do <c>Supplier</c>. Idempotente: se já está sinalizado, nova chamada é
    /// no-op silenciosa (sem evento, sem mutação). Auditoria detalhada vive no stream do Supplier.
    /// <para>
    /// Não muda <c>Status</c> nem bloqueia transições — é uma anotação para o handler de
    /// notificação proativa (Sprint 12.E, opção B+C). A Application chama este comando depois
    /// de o <c>OutdatedInstrumentDetector</c> diagnosticar divergência.
    /// </para>
    /// </summary>
    public void FlagInstrumentOutdated(string reason, DateTime occurredAt)
    {
        if (IsInstrumentOutdated)
            return; // idempotente — emite uma única vez ever

        if (string.IsNullOrWhiteSpace(reason))
            throw PayableErrors.OutdatedReasonRequired();

        Apply(new PayableInstrumentOutdated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            Reason: reason.Trim()));
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
        PaymentInstrument = PaymentInstrumentSerialization.Rebuild(
            e.InstrumentKind, e.SupplierLegalName, e.SupplierTaxIdValue, e.SupplierTaxIdType,
            e.PixKeyValue, e.PixKeyType, e.BankCode, e.Branch, e.AccountNumber, e.AccountType,
            e.EmvPayload, e.BarcodeDigits);
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
        PaymentInstrument = PaymentInstrumentSerialization.Rebuild(
            e.InstrumentKind, e.SupplierLegalName, e.SupplierTaxIdValue, e.SupplierTaxIdType,
            e.PixKeyValue, e.PixKeyType, e.BankCode, e.Branch, e.AccountNumber, e.AccountType,
            e.EmvPayload, e.BarcodeDigits);
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
        PaymentInstrument = PaymentInstrumentSerialization.Rebuild(
            e.InstrumentKind, e.SupplierLegalName, e.SupplierTaxIdValue, e.SupplierTaxIdType,
            e.PixKeyValue, e.PixKeyType, e.BankCode, e.Branch, e.AccountNumber, e.AccountType,
            e.EmvPayload, e.BarcodeDigits);
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
        Classification = new AccountRef(e.ChartOfAccountsId, e.AccountId);
        CostCenterId = e.CostCenterId;
        ClassifiedBy = e.ClassifiedBy;
        ClassifiedAt = e.OccurredAt;
        LastClassificationRuleId = null; // manual classification clears any prior rule attribution
    }

    private void When(PayableAutoClassified e)
    {
        Classification = new AccountRef(e.ChartOfAccountsId, e.AccountId);
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

    private void When(PayableMultiApprovalRequested e)
    {
        Status = PayableStatus.AwaitingApproval;
        RequiredApprovalCount = e.RequiredApprovalCount;
        EligibleApproverRoles = e.EligibleApproverRoles
            .Select(r => r.Trim().ToUpperInvariant())
            .ToList()
            .AsReadOnly();
        _approvalsReceived.Clear();
    }

    private void When(PayableApprovalRecorded e)
    {
        _approvalsReceived.Add(new ApprovalRecord(e.ApprovedBy, e.Role, e.OccurredAt));
    }

    private void When(PayableFullyApproved e)
    {
        Status = PayableStatus.Approved;
        ApprovedAt = e.OccurredAt;
    }

    private void When(PayablePaymentRequested e)
    {
        Status = PayableStatus.PaymentRequested;
        // PaymentMethod e PaymentInstrument foram populados em When(PayableCreated*) na Sprint 12.B
        // e não mudam — o evento PayablePaymentRequested apenas re-serializa para a Application
        // entregar ao PSP. Reidratação aqui só atualiza o ciclo (timestamp + limpeza de falha).
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

    private void When(PayableInstrumentOutdated e)
    {
        IsInstrumentOutdated = true;
        OutdatedAt = e.OccurredAt;
        OutdatedReason = e.Reason;
    }
}
