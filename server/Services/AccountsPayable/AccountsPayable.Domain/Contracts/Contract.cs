namespace AccountsPayable.Domain.Contracts;

using AccountsPayable.Domain.Contracts.Enumerations;
using AccountsPayable.Domain.Contracts.Events;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Aggregate Root for recurring supplier contracts (aluguel, assinaturas, etc.). Traditional
/// snapshot-persisted. Each active contract is the source for generating
/// <c>ExpectedRecurringBill</c> aggregates (via the <c>RecurringBillGenerator</c> Domain Service —
/// the Contract Aggregate itself does not reach across boundaries).
/// <para>
/// State machine: <c>Draft → Active → (Suspended ⇄ Active) → Terminated (terminal)</c>.
/// Direct <c>Draft → Terminated</c> is allowed to cancel a not-yet-activated contract.
/// </para>
/// </summary>
public sealed class Contract : AggregateRoot<ContractId>
{
    public const int MIN_PAYMENT_DAY = 1;
    public const int MAX_PAYMENT_DAY = 31;

    public TenantId TenantId { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public Description Description { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public Money MonthlyAmount { get; private set; } = default!;
    public int PaymentDay { get; private set; }
    public bool AutoCreatePayable { get; private set; }
    public ContractStatus Status { get; private set; } = default!;
    public string? TerminationReason { get; private set; }
    public string? SuspensionReason { get; private set; }

    private Contract() : base() { }

    private Contract(ContractId id) : base(id) { }

    public static Contract Create(
        ContractId id,
        TenantId tenantId,
        SupplierId supplierId,
        Description description,
        DateOnly startDate,
        DateOnly? endDate,
        Money monthlyAmount,
        int paymentDay,
        bool autoCreatePayable,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(monthlyAmount);
        if (paymentDay < MIN_PAYMENT_DAY || paymentDay > MAX_PAYMENT_DAY)
            throw ContractErrors.PaymentDayOutOfRange(paymentDay);
        if (endDate is { } end && end < startDate)
            throw ContractErrors.EndDateBeforeStartDate(startDate, end);

        var contract = new Contract(id)
        {
            TenantId = tenantId,
            SupplierId = supplierId,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            MonthlyAmount = monthlyAmount,
            PaymentDay = paymentDay,
            AutoCreatePayable = autoCreatePayable,
            Status = ContractStatus.Draft,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        contract.AddDomainEvent(new ContractCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            ContractId: id,
            SupplierId: supplierId,
            StartDate: startDate,
            EndDate: endDate,
            MonthlyAmountValue: monthlyAmount.Amount,
            MonthlyAmountCurrency: monthlyAmount.Currency.Name,
            PaymentDay: paymentDay,
            AutoCreatePayable: autoCreatePayable,
            Description: description.Value));

        return contract;
    }

    public void Activate(DateTime occurredAt)
    {
        // Activate é estritamente Draft→Active. Suspended→Active flui via Resume e emite outro evento
        // (ContractResumed), pra deixar histórico de auditoria distinguível entre "primeira ativação"
        // e "retomada após pausa".
        if (Status != ContractStatus.Draft)
            throw ContractErrors.InvalidStatusTransition(Status.Name, ContractStatus.Active.Name);

        Status = ContractStatus.Active;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ContractActivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ContractId: Id));
    }

    public void Suspend(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw ContractErrors.SuspensionReasonRequired();
        if (!Status.CanTransitionTo(ContractStatus.Suspended))
            throw ContractErrors.InvalidStatusTransition(Status.Name, ContractStatus.Suspended.Name);

        Status = ContractStatus.Suspended;
        SuspensionReason = reason.Trim();
        UpdatedAt = occurredAt;

        AddDomainEvent(new ContractSuspended(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ContractId: Id,
            Reason: reason.Trim()));
    }

    public void Resume(DateTime occurredAt)
    {
        if (Status != ContractStatus.Suspended)
            throw ContractErrors.InvalidStatusTransition(Status.Name, ContractStatus.Active.Name);

        Status = ContractStatus.Active;
        SuspensionReason = null;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ContractResumed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ContractId: Id));
    }

    public void Terminate(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw ContractErrors.TerminationReasonRequired();
        if (!Status.CanTransitionTo(ContractStatus.Terminated))
            throw ContractErrors.InvalidStatusTransition(Status.Name, ContractStatus.Terminated.Name);

        Status = ContractStatus.Terminated;
        TerminationReason = reason.Trim();
        UpdatedAt = occurredAt;

        AddDomainEvent(new ContractTerminated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ContractId: Id,
            Reason: reason.Trim()));
    }

    public void UpdateAmount(Money newAmount, DateOnly effectiveDate, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newAmount);
        if (newAmount.Currency != MonthlyAmount.Currency)
            throw ContractErrors.CurrencyCannotChange(MonthlyAmount.Currency.Name, newAmount.Currency.Name);
        if (newAmount.Amount == MonthlyAmount.Amount)
            throw ContractErrors.AmountUnchanged();

        var old = MonthlyAmount;
        MonthlyAmount = newAmount;
        UpdatedAt = occurredAt;

        AddDomainEvent(new ContractAmountChanged(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            ContractId: Id,
            OldAmountValue: old.Amount,
            NewAmountValue: newAmount.Amount,
            AmountCurrency: newAmount.Currency.Name,
            EffectiveDate: effectiveDate));
    }
}
