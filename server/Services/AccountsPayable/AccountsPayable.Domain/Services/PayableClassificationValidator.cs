namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables;

/// <summary>
/// Cross-aggregate validator (Domain Service): before <c>Payable.Classify(...)</c> the caller
/// runs this to ensure the chosen <c>Account</c> and <c>CostCenter</c> are usable for the
/// current <c>Payable</c>. Involves three aggregates — cannot live inside any one of them.
/// Stateless; no infra dependencies; receives already-loaded aggregates.
/// </summary>
public sealed class PayableClassificationValidator
{
    public void EnsureValid(
        Payable payable,
        AccountId accountId,
        ChartOfAccounts chartOfAccounts,
        CostCenter costCenter)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(chartOfAccounts);
        ArgumentNullException.ThrowIfNull(costCenter);

        if (!chartOfAccounts.TenantId.Equals(payable.TenantId))
            throw PayableClassificationErrors.TenantMismatch("ChartOfAccounts");

        if (!costCenter.TenantId.Equals(payable.TenantId))
            throw PayableClassificationErrors.TenantMismatch("CostCenter");

        var account = chartOfAccounts.Accounts.FirstOrDefault(a => a.Id.Equals(accountId))
            ?? throw PayableClassificationErrors.AccountNotFound(accountId.Value);

        if (!account.IsActive)
            throw PayableClassificationErrors.AccountInactive(accountId.Value);

        if (!account.Type.Equals(AccountType.Expense))
            throw PayableClassificationErrors.AccountTypeNotAllowed(account.Type.Name);

        if (!costCenter.IsActive)
            throw PayableClassificationErrors.CostCenterInactive(costCenter.Id.Value);
    }
}
