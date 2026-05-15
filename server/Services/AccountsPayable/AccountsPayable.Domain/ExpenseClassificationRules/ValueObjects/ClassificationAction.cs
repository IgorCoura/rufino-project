namespace AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// The classification to apply when a <see cref="ClassificationMatcher"/> hits — accounting account,
/// cost center, and a hint flag <see cref="AutoApprove"/>. The flag is consumed by the Application
/// layer (Sprint 9 doesn't auto-approve; this carries the recommendation forward to Sprint 10's
/// auto-approval policy).
/// </summary>
public sealed class ClassificationAction : ValueObject
{
    public AccountId AccountId { get; }
    public CostCenterId CostCenterId { get; }
    public bool AutoApprove { get; }

    public ClassificationAction(AccountId accountId, CostCenterId costCenterId, bool autoApprove = false)
    {
        if (accountId.Value == Guid.Empty)
            throw ClassificationActionErrors.AccountIdRequired();
        if (costCenterId.Value == Guid.Empty)
            throw ClassificationActionErrors.CostCenterIdRequired();

        AccountId = accountId;
        CostCenterId = costCenterId;
        AutoApprove = autoApprove;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccountId;
        yield return CostCenterId;
        yield return AutoApprove;
    }
}
