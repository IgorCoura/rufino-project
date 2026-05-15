namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.UnitTests.ExpenseClassificationRules.Mothers;
using AccountsPayable.UnitTests.Payables.Mothers;

public class PayableAutoClassifierTests
{
    private readonly PayableAutoClassifier _sut = new();

    // Lista de regras vazia retorna null (nada a sugerir — fica em revisão humana).
    [Fact]
    public void Decide_WithEmptyRules_ShouldReturnNull()
    {
        var payable = PayableMother.Draft();

        var decision = _sut.Decide(payable, Array.Empty<ExpenseClassificationRule>());

        Assert.Null(decision);
    }

    // Critério de aceite Sprint 9: regra inativa é ignorada mesmo que dê match.
    [Fact]
    public void Decide_WithMatchingButInactiveRule_ShouldReturnNull()
    {
        var rule = ExpenseClassificationRuleMother.Active(priority: 1);
        rule.Deactivate(ExpenseClassificationRuleMother.DEFAULT_OCCURRED_AT.AddMinutes(1));
        var payable = PayableMother.Draft();

        var decision = _sut.Decide(payable, new[] { rule });

        Assert.Null(decision);
    }

    // Regra de outro tenant é ignorada — proteção anti-IDOR no Domain.
    [Fact]
    public void Decide_WithRuleFromDifferentTenant_ShouldReturnNull()
    {
        var otherTenant = TenantId.From(new Guid("99999999-9999-9999-9999-999999999999"));
        var rule = ExpenseClassificationRuleMother.Active(tenantId: otherTenant);
        var payable = PayableMother.Draft(); // tenant default

        var decision = _sut.Decide(payable, new[] { rule });

        Assert.Null(decision);
    }

    // Uma única regra ativa que casa retorna decisão com o RuleId e a Action correspondentes.
    [Fact]
    public void Decide_WithSingleMatchingRule_ShouldReturnItsAction()
    {
        var rule = ExpenseClassificationRuleMother.Active(priority: 5);
        var payable = PayableMother.Draft();

        var decision = _sut.Decide(payable, new[] { rule });

        Assert.NotNull(decision);
        Assert.Equal(rule.Id, decision!.RuleId);
        Assert.Equal(rule.Action, decision.Action);
    }

    // Critério de aceite Sprint 9: regra com prioridade 1 vence regra com prioridade 5 quando ambas casam.
    [Fact]
    public void Decide_WithMultipleMatchingRules_ShouldReturnHighestPriority()
    {
        var ruleLow = ExpenseClassificationRuleMother.Active(
            id: ExpenseClassificationRuleId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            priority: 5);
        var ruleHigh = ExpenseClassificationRuleMother.Active(
            id: ExpenseClassificationRuleId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            priority: 1);
        var payable = PayableMother.Draft();

        var decision = _sut.Decide(payable, new[] { ruleLow, ruleHigh });

        Assert.NotNull(decision);
        Assert.Equal(ruleHigh.Id, decision!.RuleId); // priority 1 ganha de priority 5
    }

    // Critério de aceite Sprint 9: combinação SupplierId + faixa de valor + keyword é avaliada como AND.
    [Fact]
    public void Decide_WithCompositeMatch_ShouldRequireAllCriteriaToHold()
    {
        // Regra: fornecedor X + descrição "aluguel" + faixa 1000..2000.
        var match = new ClassificationMatcher(
            supplierId: PayableMother.DEFAULT_SUPPLIER,
            keyword: "aluguel",
            minAmount: new Money(1_000m, Currency.Brl),
            maxAmount: new Money(2_000m, Currency.Brl));
        var rule = ExpenseClassificationRule.CreateManual(
            ExpenseClassificationRuleId.New(),
            ExpenseClassificationRuleMother.DEFAULT_TENANT,
            match,
            ExpenseClassificationRuleMother.DefaultAction(),
            priority: 1,
            occurredAt: ExpenseClassificationRuleMother.DEFAULT_OCCURRED_AT);

        var hitting = PayableMother.Draft(description: "Aluguel sede março", amount: 1_500m);
        var missingKeyword = PayableMother.Draft(description: "Conta de luz", amount: 1_500m);
        var missingRange = PayableMother.Draft(description: "Aluguel sede", amount: 500m);

        Assert.NotNull(_sut.Decide(hitting, new[] { rule }));
        Assert.Null(_sut.Decide(missingKeyword, new[] { rule }));
        Assert.Null(_sut.Decide(missingRange, new[] { rule }));
    }
}
