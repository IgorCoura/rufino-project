namespace AccountsPayable.Domain.ExpectedRecurringBills;

using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.ExpectedRecurringBills.Enumerations;
using AccountsPayable.Domain.ExpectedRecurringBills.Events;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Aggregate Root for the "this bill should arrive this month" forecast — produced by an active
/// <see cref="Contract"/> via <c>RecurringBillGenerator</c>, matched against an incoming
/// <c>Payable</c> by <c>RecurringBillMatcher</c>. Traditional snapshot-persisted.
/// <para>
/// State machine: <c>Pending → MatchedToPayable | Missed | Cancelled (all terminal)</c>.
/// Once it leaves Pending it never comes back — re-opens would be a new aggregate.
/// </para>
/// </summary>
public sealed class ExpectedRecurringBill : AggregateRoot<ExpectedRecurringBillId>
{
    public TenantId TenantId { get; private set; }
    public ContractId ContractId { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public DateOnly ExpectedDueDate { get; private set; }
    public Money ExpectedAmount { get; private set; } = default!;
    public ExpectedRecurringBillStatus Status { get; private set; } = default!;
    public PayableId? MatchedPayableId { get; private set; }
    public DateTime? MatchedAt { get; private set; }
    public DateTime? MissedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private ExpectedRecurringBill() : base() { }

    private ExpectedRecurringBill(ExpectedRecurringBillId id) : base(id) { }

    public static ExpectedRecurringBill ForContract(
        ExpectedRecurringBillId id,
        TenantId tenantId,
        ContractId contractId,
        SupplierId supplierId,
        DateOnly expectedDueDate,
        Money expectedAmount,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(expectedAmount);

        var bill = new ExpectedRecurringBill(id)
        {
            TenantId = tenantId,
            ContractId = contractId,
            SupplierId = supplierId,
            ExpectedDueDate = expectedDueDate,
            ExpectedAmount = expectedAmount,
            Status = ExpectedRecurringBillStatus.Pending,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        bill.AddDomainEvent(new ExpectedRecurringBillCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            BillId: id,
            ContractId: contractId,
            SupplierId: supplierId,
            ExpectedDueDate: expectedDueDate,
            ExpectedAmountValue: expectedAmount.Amount,
            ExpectedAmountCurrency: expectedAmount.Currency.Name));

        return bill;
    }

    public void MatchToPayable(PayableId payableId, DateTime occurredAt)
    {
        if (Status != ExpectedRecurringBillStatus.Pending)
            throw ExpectedRecurringBillErrors.NotPending(Status.Name);

        Status = ExpectedRecurringBillStatus.MatchedToPayable;
        MatchedPayableId = payableId;
        MatchedAt = occurredAt;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpectedRecurringBillMatched(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            BillId: Id,
            MatchedPayableId: payableId));
    }

    public void MarkMissed(DateTime occurredAt)
    {
        if (Status != ExpectedRecurringBillStatus.Pending)
            throw ExpectedRecurringBillErrors.NotPending(Status.Name);

        Status = ExpectedRecurringBillStatus.Missed;
        MissedAt = occurredAt;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpectedRecurringBillMissed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            BillId: Id));
    }

    public void Cancel(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw ExpectedRecurringBillErrors.CancellationReasonRequired();
        if (Status != ExpectedRecurringBillStatus.Pending)
            throw ExpectedRecurringBillErrors.NotPending(Status.Name);

        Status = ExpectedRecurringBillStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = occurredAt;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ExpectedRecurringBillCancelled(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            BillId: Id,
            Reason: reason.Trim()));
    }
}
