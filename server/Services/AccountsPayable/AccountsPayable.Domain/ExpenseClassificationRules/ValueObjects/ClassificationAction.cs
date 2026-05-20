namespace AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;

using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// The classification to apply when a <see cref="ClassificationMatcher"/> hits — accounting account
/// (anchored to its <c>ChartOfAccounts</c> via <see cref="AccountRef"/>), cost center, and a hint
/// flag <see cref="AutoApprove"/>. The flag is consumed by the Application layer (Sprint 9 doesn't
/// auto-approve; this carries the recommendation forward to Sprint 10's auto-approval policy).
/// </summary>
public sealed class ClassificationAction : ValueObject
{
    public AccountRef Account { get; }
    public CostCenterId CostCenterId { get; }
    public bool AutoApprove { get; }

    public ClassificationAction(AccountRef account, CostCenterId costCenterId, bool autoApprove = false)
    {
        if (account is null)
            throw ClassificationActionErrors.AccountRefRequired();
        if (costCenterId.Value == Guid.Empty)
            throw ClassificationActionErrors.CostCenterIdRequired();

        Account = account;
        CostCenterId = costCenterId;
        AutoApprove = autoApprove;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Account;
        yield return CostCenterId;
        yield return AutoApprove;
    }
}
