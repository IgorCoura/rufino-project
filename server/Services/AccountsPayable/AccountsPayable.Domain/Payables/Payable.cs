namespace AccountsPayable.Domain.Payables;

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

    public void Schedule(DateOnly scheduledFor, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(PayableStatus.Scheduled))
            throw PayableErrors.InvalidStatusTransition(Status.Name, PayableStatus.Scheduled.Name);

        Apply(new PayableScheduled(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            PayableId: Id,
            ScheduledFor: scheduledFor));
    }

    public void MarkAsPaidManually(PaymentProof proof, DateTime paidAt, DateTime occurredAt)
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
}
