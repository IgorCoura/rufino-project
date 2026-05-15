namespace AccountsPayable.UnitTests.ExpenseClassificationRules.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class ClassificationActionTests
{
    private static readonly AccountId VALID_ACCOUNT = AccountId.From(new Guid("33333333-3333-3333-3333-333333333333"));
    private static readonly CostCenterId VALID_COST_CENTER = CostCenterId.From(new Guid("44444444-4444-4444-4444-444444444444"));

    // Construtor com Ids válidos e autoApprove default=false retorna instância coerente.
    [Fact]
    public void Construct_WithValidIds_ShouldPersistFields()
    {
        var action = new ClassificationAction(VALID_ACCOUNT, VALID_COST_CENTER);

        Assert.Equal(VALID_ACCOUNT, action.AccountId);
        Assert.Equal(VALID_COST_CENTER, action.CostCenterId);
        Assert.False(action.AutoApprove);
    }

    // autoApprove=true é refletido no estado para a Application consumir como hint para Sprint 10.
    [Fact]
    public void Construct_WithAutoApproveTrue_ShouldPersistFlag()
    {
        var action = new ClassificationAction(VALID_ACCOUNT, VALID_COST_CENTER, autoApprove: true);

        Assert.True(action.AutoApprove);
    }

    // AccountId vazio lança AP.CAT01 — ação sem conta contábil não classifica nada.
    [Fact]
    public void Construct_WithEmptyAccountId_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ClassificationAction(AccountId.Empty, VALID_COST_CENTER));

        Assert.Equal("AP.CAT01", ex.Id);
    }

    // CostCenterId vazio lança AP.CAT02 — ação sem centro de custo não classifica nada.
    [Fact]
    public void Construct_WithEmptyCostCenterId_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ClassificationAction(VALID_ACCOUNT, CostCenterId.Empty));

        Assert.Equal("AP.CAT02", ex.Id);
    }

    // Duas instâncias com mesmos componentes são iguais (VO).
    [Fact]
    public void Equality_ShouldBeStructural()
    {
        var a = new ClassificationAction(VALID_ACCOUNT, VALID_COST_CENTER, autoApprove: true);
        var b = new ClassificationAction(VALID_ACCOUNT, VALID_COST_CENTER, autoApprove: true);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
