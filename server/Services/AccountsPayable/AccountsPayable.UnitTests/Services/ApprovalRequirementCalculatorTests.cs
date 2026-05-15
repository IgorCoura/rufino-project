namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.AutoApprovalPolicies;
using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.Services;
using AccountsPayable.UnitTests.AutoApprovalPolicies.Mothers;
using AccountsPayable.UnitTests.Payables.Mothers;

public class ApprovalRequirementCalculatorTests
{
    private readonly ApprovalRequirementCalculator _sut = new();

    // Critério de aceite Sprint 10: conta sem regra que case cai no default (sempre aprovar manualmente).
    [Fact]
    public void Calculate_WithEmptyPolicy_ShouldReturnDefaultManual()
    {
        var payable = PayableMother.Classified();
        var policy = AutoApprovalPolicyMother.Empty();

        var req = _sut.Calculate(payable, policy);

        Assert.Same(ApprovalRequirement.DefaultManual, req);
        Assert.True(req.Required);
        Assert.Equal(1, req.RequiredCount);
    }

    // Payable abaixo do ThresholdAmount da regra é tratado como "rule out of scope" → default manual.
    [Fact]
    public void Calculate_WithAmountBelowThreshold_ShouldReturnDefaultManual()
    {
        var payable = PayableMother.Classified(amount: 5_000m);
        var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out _, threshold: 10_000m);

        var req = _sut.Calculate(payable, policy);

        Assert.Same(ApprovalRequirement.DefaultManual, req);
    }

    // Payable acima do ThresholdAmount com regra ativa que casa → retorna requisitos da regra.
    [Fact]
    public void Calculate_WithMatchingRule_ShouldReturnRulesRequirement()
    {
        var payable = PayableMother.Classified(amount: 15_000m);
        var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out _, threshold: 10_000m, requiredCount: 2);

        var req = _sut.Calculate(payable, policy);

        Assert.True(req.Required);
        Assert.Equal(2, req.RequiredCount);
        Assert.Equal(new[] { "OWNER", "PARTNER" }, req.EligibleRoles);
    }

    // Critério de aceite Sprint 10: conta que casa em duas regras usa a mais restritiva (RequiredApprovalCount maior ganha).
    [Fact]
    public void Calculate_WithTwoMatchingRules_ShouldReturnMostRestrictive()
    {
        var payable = PayableMother.Classified(amount: 60_000m);
        var policy = AutoApprovalPolicyMother.Empty();
        policy.AddRule(
            ApprovalRuleId.New(),
            new ApprovalMatchCriteria(minAmount: new Money(1m, Currency.Brl)),
            thresholdAmount: new Money(10_000m, Currency.Brl),
            requiredApproverRoles: new ApproverRoles(new[] { "PARTNER" }),
            requiredApprovalCount: 2,
            occurredAt: AutoApprovalPolicyMother.DEFAULT_OCCURRED_AT.AddMinutes(1));
        policy.AddRule(
            ApprovalRuleId.New(),
            new ApprovalMatchCriteria(minAmount: new Money(1m, Currency.Brl)),
            thresholdAmount: new Money(50_000m, Currency.Brl),
            requiredApproverRoles: new ApproverRoles(new[] { "OWNER", "PARTNER" }),
            requiredApprovalCount: 3, // mais restritiva
            occurredAt: AutoApprovalPolicyMother.DEFAULT_OCCURRED_AT.AddMinutes(2));

        var req = _sut.Calculate(payable, policy);

        Assert.Equal(3, req.RequiredCount);
        Assert.Equal(new[] { "OWNER", "PARTNER" }, req.EligibleRoles);
    }

    // Regra inativa é ignorada mesmo que daria match.
    [Fact]
    public void Calculate_WithMatchingButInactiveRule_ShouldFallBackToDefault()
    {
        var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out var ruleId, threshold: 10_000m);
        policy.DeactivateRule(ruleId, AutoApprovalPolicyMother.DEFAULT_OCCURRED_AT.AddMinutes(5));
        var payable = PayableMother.Classified(amount: 15_000m);

        var req = _sut.Calculate(payable, policy);

        Assert.Same(ApprovalRequirement.DefaultManual, req);
    }
}
