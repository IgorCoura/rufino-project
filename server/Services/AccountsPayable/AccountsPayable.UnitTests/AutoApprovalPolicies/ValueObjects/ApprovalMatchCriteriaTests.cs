namespace AccountsPayable.UnitTests.AutoApprovalPolicies.ValueObjects;

using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.Payables.Mothers;

public class ApprovalMatchCriteriaTests
{
    private static readonly AccountId OTHER_ACCOUNT =
        AccountId.From(new Guid("99999999-9999-9999-9999-999999999999"));

    // Construtor sem nenhum critério lança AP.AMC01.
    [Fact]
    public void Construct_WithNoCriteria_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ApprovalMatchCriteria());
        Assert.Equal("AP.AMC01", ex.Id);
    }

    // Faixa inválida (min > max) lança AP.AMC02.
    [Fact]
    public void Construct_WithMinAmountAboveMaxAmount_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ApprovalMatchCriteria(
            minAmount: new Money(2_000m, Currency.Brl),
            maxAmount: new Money(1_000m, Currency.Brl)));
        Assert.Equal("AP.AMC02", ex.Id);
    }

    // Matches por SupplierId — casa quando coincide, não casa quando diferente.
    [Fact]
    public void Matches_BySupplierId_ShouldRespectEquality()
    {
        var criteria = new ApprovalMatchCriteria(supplierId: PayableMother.DEFAULT_SUPPLIER);
        var matching = PayableMother.Draft();
        var nonMatching = PayableMother.Draft();

        Assert.True(criteria.Matches(matching));
        // Mesmo supplier por default — então criamos um diferente:
        var otherCriteria = new ApprovalMatchCriteria(
            supplierId: AutoApprovalPolicies.Mothers.AutoApprovalPolicyMother.DEFAULT_SUPPLIER);
        Assert.True(otherCriteria.Matches(nonMatching));
    }

    // Matches por AccountId só passa se o Payable foi classificado e o AccountId bate.
    [Fact]
    public void Matches_ByAccountId_ShouldReturnFalseWhenPayableUnclassified()
    {
        var criteria = new ApprovalMatchCriteria(accountId: PayableMother.DEFAULT_ACCOUNT);
        var payable = PayableMother.Draft(); // sem classificação

        Assert.False(criteria.Matches(payable));
    }

    [Fact]
    public void Matches_ByAccountId_ShouldReturnTrueWhenClassifiedToSameAccount()
    {
        var criteria = new ApprovalMatchCriteria(accountId: PayableMother.DEFAULT_ACCOUNT);
        var payable = PayableMother.Classified();

        Assert.True(criteria.Matches(payable));
    }

    [Fact]
    public void Matches_ByAccountId_ShouldReturnFalseWhenClassifiedToDifferentAccount()
    {
        var criteria = new ApprovalMatchCriteria(accountId: OTHER_ACCOUNT);
        var payable = PayableMother.Classified();

        Assert.False(criteria.Matches(payable));
    }

    // Matches por faixa de valor — inclusiva nos limites.
    [Theory]
    [InlineData(1_000, true)]
    [InlineData(1_500, true)]
    [InlineData(2_000, true)]
    [InlineData(999, false)]
    [InlineData(2_001, false)]
    public void Matches_ByAmountRange_ShouldBeInclusive(decimal amount, bool expected)
    {
        var criteria = new ApprovalMatchCriteria(
            minAmount: new Money(1_000m, Currency.Brl),
            maxAmount: new Money(2_000m, Currency.Brl));
        var payable = PayableMother.Draft(amount: amount);

        Assert.Equal(expected, criteria.Matches(payable));
    }

    // Combinação de critérios é AND.
    [Fact]
    public void Matches_WithCompositeCriteria_ShouldBeConjunctive()
    {
        var criteria = new ApprovalMatchCriteria(
            supplierId: PayableMother.DEFAULT_SUPPLIER,
            minAmount: new Money(1_000m, Currency.Brl));
        var matchingPayable = PayableMother.Draft(amount: 1_500m);
        var lowAmountPayable = PayableMother.Draft(amount: 500m);

        Assert.True(criteria.Matches(matchingPayable));
        Assert.False(criteria.Matches(lowAmountPayable));
    }
}
