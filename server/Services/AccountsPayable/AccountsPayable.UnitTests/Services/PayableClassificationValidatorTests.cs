namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.ChartOfAccounts.Enumerations;
using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.ValueObjects;
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
    /// com uma Account ativa do tipo Expense e CostCenter ativo. Devolve as peças e a AccountRef
    /// anchored ao chart criado.
    /// </summary>
    private static (Payable payable, ChartOfAccounts chart, CostCenter costCenter, AccountRef accountRef) ValidScenario()
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
        return (payable, chart, costCenter, new AccountRef(chart.Id, accountId));
    }

    public class WhenAllRulesPass
    {
        // Cenário válido (AccountRef anchored ao chart, account ativa Expense, costCenter ativo, mesmo tenant) não lança.
        [Fact]
        public void EnsureValid_WithCompletelyValidScenario_ShouldNotThrow()
        {
            var (payable, chart, costCenter, accountRef) = ValidScenario();
            var validator = new PayableClassificationValidator();

            validator.EnsureValid(payable, accountRef, chart, costCenter);
        }
    }

    public class WhenAccountIsInvalid
    {
        // AccountRef anchored ao chart certo mas apontando para AccountId que não existe lança AP.PCL01.
        [Fact]
        public void EnsureValid_WithUnknownAccountId_ShouldThrowDomainException()
        {
            var (payable, chart, costCenter, _) = ValidScenario();
            var unknownRef = new AccountRef(chart.Id, AccountId.New());
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, unknownRef, chart, costCenter));

            Assert.Equal("AP.PCL01", ex.Id);
        }

        // Account inativa lança AP.PCL02 (mesmo se for do tipo correto).
        [Fact]
        public void EnsureValid_WithInactiveAccount_ShouldThrowDomainException()
        {
            var (payable, chart, costCenter, accountRef) = ValidScenario();
            chart.DeactivateAccount(accountRef.AccountId, LATER.AddMinutes(1));
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountRef, chart, costCenter));

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
                () => validator.EnsureValid(payable, new AccountRef(chart.Id, assetId), chart, costCenter));

            Assert.Equal("AP.PCL03", ex.Id);
        }
    }

    public class WhenCostCenterIsInvalid
    {
        // CostCenter inativo lança AP.PCL04.
        [Fact]
        public void EnsureValid_WithInactiveCostCenter_ShouldThrowDomainException()
        {
            var (payable, chart, _, accountRef) = ValidScenario();
            var costCenter = CostCenterMother.Inactive();
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, accountRef, chart, costCenter));

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
                () => validator.EnsureValid(payable, new AccountRef(chart.Id, accountId), chart, costCenter));

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
                () => validator.EnsureValid(payable, new AccountRef(chart.Id, accountId), chart, costCenter));

            Assert.Equal("AP.PCL05", ex.Id);
        }
    }

    public class WhenChartAnchorMismatches
    {
        // AccountRef.ChartOfAccountsId apontando para um chart diferente do passado por parâmetro lança AP.PCL06.
        // Esta é a guarda que mantém a regra DDD "referência cross-aggregate é sempre anchored ao AR": sem esta
        // verificação, dois charts diferentes poderiam compartilhar AccountIds e a validação acabaria muda.
        [Fact]
        public void EnsureValid_WithAccountRefPointingToDifferentChart_ShouldThrowDomainException()
        {
            var (payable, chart, costCenter, _) = ValidScenario();
            // AccountRef ancorado a um chart distinto do fornecido — anchor mismatch.
            var foreignRef = new AccountRef(
                ChartOfAccountsId.From(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")),
                AccountId.New());
            var validator = new PayableClassificationValidator();

            var ex = Assert.Throws<DomainException>(
                () => validator.EnsureValid(payable, foreignRef, chart, costCenter));

            Assert.Equal("AP.PCL06", ex.Id);
        }
    }

    public class WhenInputIsNull
    {
        // Argumento payable null lança ArgumentNullException (guarda básica do service).
        [Fact]
        public void EnsureValid_WithNullPayable_ShouldThrowArgumentNullException()
        {
            var (_, chart, costCenter, accountRef) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(null!, accountRef, chart, costCenter));
        }

        // Argumento accountRef null lança ArgumentNullException — guarda contra esquecimento do orquestrador.
        [Fact]
        public void EnsureValid_WithNullAccountRef_ShouldThrowArgumentNullException()
        {
            var (payable, chart, costCenter, _) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(payable, null!, chart, costCenter));
        }

        // Argumento chartOfAccounts null lança ArgumentNullException.
        [Fact]
        public void EnsureValid_WithNullChartOfAccounts_ShouldThrowArgumentNullException()
        {
            var (payable, _, costCenter, accountRef) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(payable, accountRef, null!, costCenter));
        }

        // Argumento costCenter null lança ArgumentNullException.
        [Fact]
        public void EnsureValid_WithNullCostCenter_ShouldThrowArgumentNullException()
        {
            var (payable, chart, _, accountRef) = ValidScenario();
            var validator = new PayableClassificationValidator();

            Assert.Throws<ArgumentNullException>(
                () => validator.EnsureValid(payable, accountRef, chart, null!));
        }
    }
}
