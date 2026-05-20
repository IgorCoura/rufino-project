namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;

/// <summary>
/// Cross-aggregate validator (Domain Service): before <c>Payable.Classify(...)</c> the caller
/// runs this to ensure the chosen <c>Account</c> and <c>CostCenter</c> are usable for the
/// current <c>Payable</c>. Involves three aggregates — cannot live inside any one of them.
/// Stateless; no infra dependencies; receives already-loaded aggregates.
/// <para>
/// The <see cref="AccountRef"/> arrives anchored to its owning <c>ChartOfAccounts</c>; this
/// validator checks that the supplied <c>chartOfAccounts</c> instance is the same one referenced
/// (AP.PCL06) before looking the account up internally.
/// </para>
/// </summary>
public sealed class PayableClassificationValidator
{
    public void EnsureValid(
        Payable payable,
        AccountRef accountRef,
        ChartOfAccounts chartOfAccounts,
        CostCenter costCenter)
    {
        ArgumentNullException.ThrowIfNull(payable);
        ArgumentNullException.ThrowIfNull(accountRef);
        ArgumentNullException.ThrowIfNull(chartOfAccounts);
        ArgumentNullException.ThrowIfNull(costCenter);

        if (!chartOfAccounts.TenantId.Equals(payable.TenantId))
            throw PayableClassificationErrors.TenantMismatch("ChartOfAccounts");

        if (!costCenter.TenantId.Equals(payable.TenantId))
            throw PayableClassificationErrors.TenantMismatch("CostCenter");

        if (!chartOfAccounts.Id.Equals(accountRef.ChartOfAccountsId))
            throw PayableClassificationErrors.ChartMismatch(
                expectedChartId: accountRef.ChartOfAccountsId.Value,
                actualChartId: chartOfAccounts.Id.Value);

        var account = chartOfAccounts.Accounts.FirstOrDefault(a => a.Id.Equals(accountRef.AccountId))
            ?? throw PayableClassificationErrors.AccountNotFound(accountRef.AccountId.Value);

        if (!account.IsActive)
            throw PayableClassificationErrors.AccountInactive(accountRef.AccountId.Value);

        if (!account.Type.Equals(AccountType.Expense))
            throw PayableClassificationErrors.AccountTypeNotAllowed(account.Type.Name);

        if (!costCenter.IsActive)
            throw PayableClassificationErrors.CostCenterInactive(costCenter.Id.Value);
    }
}
