namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.UnitTests.ChartOfAccounts.Mothers;
using AccountsPayable.UnitTests.CostCenters.Mothers;
using AccountsPayable.UnitTests.Payables.Mothers;

public class PayableClassificationValidatorTests
{
    private static readonly DateTime FIXED_NOW = PayableMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(1);

    /// <summary>
    /// Constrói um cenário coerente: Payable + ChartOfAccounts + CostCenter, todos no mesmo tenant,
    /// com uma Account ativa do tipo Expense e CostCenter ativo. Devolve as peças e o id da account válida.
    /// </summary>
    private static (Payable payable, ChartOfAccounts chart, CostCenter costCenter, AccountId expenseAccountId) ValidScenario()
    {
        var tenant = PayableMother.DEFAULT_TENANT;
        var payable = PayableMother.Draft(tenantId: tenant);
        var chart = ChartOfAccountsMother.Empty(tenantId: tenant);
        var accountId = AccountId.New();
        chart.AddAccount(
            id: accountId, parentId: null,
            code: new AccountCode("4"),
            name: new AccountName("Despesas"),
            type: AccountType.Expense,
            occurredAt: LATER);
        var costCenter = CostCenterMother.Active(tenantId: tenant);
        return (payable, chart, costCenter, accountId);
    }

    public class WhenAllRulesPass
    {
        // Cenário válido (account ativa Expense, costCenter ativo, mesmo tenant) não lança.
        [Fact]
        public void EnsureValid_WithCompletelyValidScenario_ShouldNotThrow()
        {
            var (payable, chart, costCenter, accountId) = ValidScenario();
            var validator = new PayableClassificationValidator();

            validator.EnsureValid(payable, accountId, chart, costCenter);
        }
    }

    public class WhenAccountIsInvalid
    {
        // AccountId que não existe no ChartOfAccounts lança AP.PCL01.
        [Fact]
        public void EnsureValid_WithUnknownAccountId_ShouldThrowDomainException()
        {
            var (payable, chart, costCenter, _) = ValidScenario();
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, AccountId.New(), chart, costCenter));

            Assert.Equal("AP.PCL01", ex.Id);
        }

        // Account inativa lança AP.PCL02 (mesmo se for do tipo correto).
        [Fact]
        public void EnsureValid_WithInactiveAccount_ShouldThrowDomainException()
        {
            var (payable, chart, costCenter, accountId) = ValidScenario();
            chart.DeactivateAccount(accountId, LATER.AddMinutes(1));
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountId, chart, costCenter));

            Assert.Equal("AP.PCL02", ex.Id);
        }

        // Account de tipo não-Expense (ex.: Asset) lança AP.PCL03.
        [Theory]
        [InlineData("ASSET")]
        [InlineData("LIABILITY")]
        [InlineData("REVENUE")]
        [InlineData("EQUITY")]
        public void EnsureValid_WithNonExpenseAccount_ShouldThrowDomainException(string typeName)
        {
            var tenant = PayableMother.DEFAULT_TENANT;
            var payable = PayableMother.Draft(tenantId: tenant);
            var chart = ChartOfAccountsMother.Empty(tenantId: tenant);
            var assetId = AccountId.New();
            chart.AddAccount(
                id: assetId, parentId: null,
                code: new AccountCode("1"),
                name: new AccountName($"Conta {typeName}"),
                type: Enumeration.FromDisplayName<AccountType>(typeName),
                occurredAt: LATER);
            var costCenter = CostCenterMother.Active(tenantId: tenant);
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, assetId, chart, costCenter));

            Assert.Equal("AP.PCL03", ex.Id);
        }
    }

    public class WhenCostCenterIsInvalid
    {
        // CostCenter inativo lança AP.PCL04.
        [Fact]
        public void EnsureValid_WithInactiveCostCenter_ShouldThrowDomainException()
        {
            var (payable, chart, _, accountId) = ValidScenario();
            var costCenter = CostCenterMother.Inactive();
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountId, chart, costCenter));

            Assert.Equal("AP.PCL04", ex.Id);
        }
    }

    public class WhenTenantsMismatch
    {
        // ChartOfAccounts de outro tenant lança AP.PCL05 — proteção IDOR (critério da Sprint 4).
        [Fact]
        public void EnsureValid_WithChartFromOtherTenant_ShouldThrowDomainException()
        {
            var payableTenant = PayableMother.DEFAULT_TENANT;
            var otherTenant = TenantId.From(new Guid("99999999-9999-9999-9999-999999999999"));
            var payable = PayableMother.Draft(tenantId: payableTenant);
            var chart = ChartOfAccountsMother.Empty(tenantId: otherTenant);
            var accountId = AccountId.New();
            chart.AddAccount(accountId, null, new AccountCode("4"), new AccountName("Despesas"), AccountType.Expense, LATER);
            var costCenter = CostCenterMother.Active(tenantId: payableTenant);
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountId, chart, costCenter));

            Assert.Equal("AP.PCL05", ex.Id);
        }

        // CostCenter de outro tenant lança AP.PCL05 — proteção IDOR (critério da Sprint 4).
        [Fact]
        public void EnsureValid_WithCostCenterFromOtherTenant_ShouldThrowDomainException()
        {
            var payableTenant = PayableMother.DEFAULT_TENANT;
            var otherTenant = TenantId.From(new Guid("99999999-9999-9999-9999-999999999999"));
            var payable = PayableMother.Draft(tenantId: payableTenant);
            var chart = ChartOfAccountsMother.Empty(tenantId: payableTenant);
            var accountId = AccountId.New();
            chart.AddAccount(accountId, null, new AccountCode("4"), new AccountName("Despesas"), AccountType.Expense, LATER);
            var costCenter = CostCenterMother.Active(tenantId: otherTenant);
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountId, chart, costCenter));

            Assert.Equal("AP.PCL05", ex.Id);
        }
    }

    public class WhenInputIsNull
    {
        // Argumento payable null lança ArgumentNullException (guarda básica do service).
        [Fact]
        public void EnsureValid_WithNullPayable_ShouldThrowArgumentNullException()
        {
            var (_, chart, costCenter, accountId) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(null!, accountId, chart, costCenter));
        }

        // Argumento chartOfAccounts null lança ArgumentNullException.
        [Fact]
        public void EnsureValid_WithNullChartOfAccounts_ShouldThrowArgumentNullException()
        {
            var (payable, _, costCenter, accountId) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(payable, accountId, null!, costCenter));
        }

        // Argumento costCenter null lança ArgumentNullException.
        [Fact]
        public void EnsureValid_WithNullCostCenter_ShouldThrowArgumentNullException()
        {
            var (payable, chart, _, accountId) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(payable, accountId, chart, null!));
        }
    }
}
