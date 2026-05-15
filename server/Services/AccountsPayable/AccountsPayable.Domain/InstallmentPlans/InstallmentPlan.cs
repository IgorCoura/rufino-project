namespace AccountsPayable.Domain.InstallmentPlans;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.InstallmentPlans.Enumerations;
using AccountsPayable.Domain.InstallmentPlans.Events;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Aggregate Root for installment plans (parcelamentos). Traditional snapshot-persisted Aggregate
/// — acts only as a grouper. Each installment is a separate <see cref="Payable"/> Aggregate that
/// references back via <c>InstallmentPlanId</c>; cents-correct amount distribution and date spacing
/// is the responsibility of the <c>InstallmentPlanFactory</c> Domain Service.
/// <para>
/// Cancelling the plan emits <see cref="InstallmentPlanCancelled"/> carrying the list of linked
/// <see cref="PayableId"/>s. The Application layer consumes the event and calls <c>Payable.Cancel</c>
/// on each non-terminal payable — payables already <c>Paid</c> are left intact.
/// </para>
/// </summary>
public sealed class InstallmentPlan : AggregateRoot<InstallmentPlanId>
{
    public const int MIN_INSTALLMENT_COUNT = 2;

    private readonly List<PayableLink> _links = [];

    public TenantId TenantId { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public Money TotalAmount { get; private set; } = default!;
    public int InstallmentCount { get; private set; }
    public DateOnly FirstDueDate { get; private set; }
    public InstallmentFrequency Frequency { get; private set; } = default!;
    public Description Description { get; private set; } = default!;
    public InstallmentPlanStatus Status { get; private set; } = default!;
    public string? CancellationReason { get; private set; }

    public IReadOnlyList<PayableId> PayableIds => _links.Select(l => l.PayableId).ToList().AsReadOnly();

    private InstallmentPlan() : base() { }

    private InstallmentPlan(InstallmentPlanId id) : base(id) { }

    public static InstallmentPlan Create(
        InstallmentPlanId id,
        TenantId tenantId,
        SupplierId supplierId,
        Money totalAmount,
        int installmentCount,
        DateOnly firstDueDate,
        InstallmentFrequency frequency,
        Description description,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(totalAmount);
        ArgumentNullException.ThrowIfNull(frequency);
        ArgumentNullException.ThrowIfNull(description);

        if (installmentCount < MIN_INSTALLMENT_COUNT)
            throw InstallmentPlanErrors.InstallmentCountTooLow(installmentCount);

        var plan = new InstallmentPlan(id)
        {
            TenantId = tenantId,
            SupplierId = supplierId,
            TotalAmount = totalAmount,
            InstallmentCount = installmentCount,
            FirstDueDate = firstDueDate,
            Frequency = frequency,
            Description = description,
            Status = InstallmentPlanStatus.Active,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        plan.AddDomainEvent(new InstallmentPlanCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            InstallmentPlanId: id,
            SupplierId: supplierId,
            TotalAmountValue: totalAmount.Amount,
            TotalAmountCurrency: totalAmount.Currency.Name,
            InstallmentCount: installmentCount,
            FirstDueDate: firstDueDate,
            Frequency: frequency.Name,
            Description: description.Value));

        return plan;
    }

    public void RegisterPayable(PayableId payableId, int installmentNumber, DateTime occurredAt)
    {
        if (Status == InstallmentPlanStatus.Cancelled)
            throw InstallmentPlanErrors.CannotRegisterOnCancelled();
        if (installmentNumber < 1 || installmentNumber > InstallmentCount)
            throw InstallmentPlanErrors.InstallmentNumberOutOfRange(installmentNumber, InstallmentCount);
        if (_links.Any(l => l.InstallmentNumber == installmentNumber))
            throw InstallmentPlanErrors.InstallmentNumberAlreadyRegistered(installmentNumber);

        _links.Add(new PayableLink(payableId, installmentNumber));
        UpdatedAt = occurredAt;

        AddDomainEvent(new PayableLinkedToInstallmentPlan(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            InstallmentPlanId: Id,
            PayableId: payableId,
            InstallmentNumber: installmentNumber));
    }

    public void Cancel(string reason, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw InstallmentPlanErrors.CancellationReasonRequired();
        if (Status == InstallmentPlanStatus.Cancelled)
            throw InstallmentPlanErrors.AlreadyCancelled();

        Status = InstallmentPlanStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAt = occurredAt;

        AddDomainEvent(new InstallmentPlanCancelled(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            InstallmentPlanId: Id,
            Reason: reason.Trim(),
            LinkedPayableIds: PayableIds));
    }

    private sealed record PayableLink(PayableId PayableId, int InstallmentNumber);
}
