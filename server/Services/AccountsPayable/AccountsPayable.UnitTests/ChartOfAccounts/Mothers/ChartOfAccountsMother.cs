namespace AccountsPayable.UnitTests.ChartOfAccounts.Mothers;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public static class ChartOfAccountsMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));

    public static ChartOfAccounts Empty(
        ChartOfAccountsId? id = null,
        TenantId? tenantId = null,
        string name = "Plano Padrão")
        => ChartOfAccounts.Create(
            id: id ?? ChartOfAccountsId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            name: new ChartOfAccountsName(name),
            occurredAt: DEFAULT_OCCURRED_AT);

    /// <summary>
    /// Two-level tree: one root "4" Expense, one child "4.01" under it.
    /// Returns the chart and the ids of root/child for tests that need them.
    /// </summary>
    public static (ChartOfAccounts chart, AccountId rootId, AccountId childId) WithRootAndOneChild()
    {
        var chart = Empty();
        var rootId = AccountId.New();
        var childId = AccountId.New();

        chart.AddAccount(rootId, parentId: null,
            code: new AccountCode("4"),
            name: new AccountName("Despesas"),
            type: AccountType.Expense,
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(1));

        chart.AddAccount(childId, parentId: rootId,
            code: new AccountCode("4.01"),
            name: new AccountName("Despesas operacionais"),
            type: AccountType.Expense,
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(2));

        return (chart, rootId, childId);
    }

    /// <summary>
    /// Linear chain of accounts at the requested depth: "4" → "4.1" → "4.1.1" → ...
    /// Returns the chart and the id of the deepest leaf.
    /// </summary>
    public static (ChartOfAccounts chart, AccountId deepestId) ChainOfDepth(int depth)
    {
        var chart = Empty();
        AccountId? parentId = null;
        var deepest = AccountId.Empty;
        var code = string.Empty;

        for (var level = 1; level <= depth; level++)
        {
            code = level == 1 ? "4" : code + ".1";
            var id = AccountId.New();
            chart.AddAccount(id, parentId,
                code: new AccountCode(code),
                name: new AccountName($"Nível {level}"),
                type: AccountType.Expense,
                occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(level));
            parentId = id;
            deepest = id;
        }

        return (chart, deepest);
    }
}
