namespace AccountsPayable.UnitTests.AutoApprovalPolicies;

using AccountsPayable.Domain.AutoApprovalPolicies;
using AccountsPayable.Domain.AutoApprovalPolicies.Entities;
using AccountsPayable.Domain.AutoApprovalPolicies.Events;
using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.AutoApprovalPolicies.Mothers;

public class AutoApprovalPolicyTests
{
    private static readonly DateTime FIXED_NOW = AutoApprovalPolicyMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta política vazia (sem regras) e emite AutoApprovalPolicyCreated.
        [Fact]
        public void Create_ShouldStartEmptyAndEmitEvent()
        {
            var policy = AutoApprovalPolicyMother.Empty();

            Assert.Empty(policy.Rules);
            Assert.Equal(AutoApprovalPolicyMother.DEFAULT_TENANT, policy.TenantId);
            var created = Assert.IsType<AutoApprovalPolicyCreated>(policy.PullDomainEvents().Single());
            Assert.Equal(policy.Id, created.PolicyId);
        }
    }

    public class WhenAddingRule
    {
        // AddRule adiciona ApprovalRule à coleção interna, retorna a entity criada e emite ApprovalRuleAdded.
        [Fact]
        public void AddRule_OnEmptyPolicy_ShouldAppendRuleAndEmitEvent()
        {
            var policy = AutoApprovalPolicyMother.Empty();
            policy.PullDomainEvents();
            var ruleId = ApprovalRuleId.New();

            var added = policy.AddRule(
                ruleId: ruleId,
                matchCriteria: AutoApprovalPolicyMother.SupplierCriteria(),
                thresholdAmount: new Money(10_000m, Currency.Brl),
                requiredApproverRoles: AutoApprovalPolicyMother.Partners(),
                requiredApprovalCount: 2,
                occurredAt: LATER);

            Assert.Single(policy.Rules);
            Assert.Equal(ruleId, added.Id);
            Assert.True(added.IsActive);
            Assert.Equal(2, added.RequiredApprovalCount);
            var evt = Assert.IsType<ApprovalRuleAdded>(policy.PullDomainEvents().Single());
            Assert.Equal(ruleId, evt.RuleId);
            Assert.Equal(2, evt.RequiredApprovalCount);
            Assert.Equal(new[] { "OWNER", "PARTNER" }, evt.RequiredApproverRoles);
        }

        // AddRule com requiredApprovalCount < 1 lança AP.AAP01.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AddRule_WithNonPositiveCount_ShouldThrowDomainException(int count)
        {
            var policy = AutoApprovalPolicyMother.Empty();

            var ex = Assert.Throws<DomainException>(() => policy.AddRule(
                ApprovalRuleId.New(),
                AutoApprovalPolicyMother.SupplierCriteria(),
                new Money(10_000m, Currency.Brl),
                AutoApprovalPolicyMother.Partners(),
                requiredApprovalCount: count,
                LATER));

            Assert.Equal("AP.AAP01", ex.Id);
        }
    }

    public class WhenRemovingRule
    {
        // RemoveRule remove a entity e emite ApprovalRuleRemoved.
        [Fact]
        public void RemoveRule_OnExistingRule_ShouldRemoveAndEmitEvent()
        {
            var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out var ruleId);
            policy.PullDomainEvents();

            policy.RemoveRule(ruleId, LATER);

            Assert.Empty(policy.Rules);
            var evt = Assert.IsType<ApprovalRuleRemoved>(policy.PullDomainEvents().Single());
            Assert.Equal(ruleId, evt.RuleId);
        }

        // RemoveRule com Id inexistente lança AP.AAP02.
        [Fact]
        public void RemoveRule_OnUnknownRule_ShouldThrowDomainException()
        {
            var policy = AutoApprovalPolicyMother.Empty();

            var ex = Assert.Throws<DomainException>(() => policy.RemoveRule(ApprovalRuleId.New(), LATER));

            Assert.Equal("AP.AAP02", ex.Id);
        }
    }

    public class WhenActivatingAndDeactivatingRule
    {
        // DeactivateRule flipa IsActive e emite ApprovalRuleDeactivated.
        [Fact]
        public void DeactivateRule_OnActiveRule_ShouldFlipAndEmitEvent()
        {
            var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out var ruleId);
            policy.PullDomainEvents();

            policy.DeactivateRule(ruleId, LATER);

            Assert.False(policy.Rules.Single().IsActive);
            Assert.IsType<ApprovalRuleDeactivated>(policy.PullDomainEvents().Single());
        }

        // ActivateRule em regra já ativa lança AP.AAP03.
        [Fact]
        public void ActivateRule_WhenAlreadyActive_ShouldThrowDomainException()
        {
            var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out var ruleId);

            var ex = Assert.Throws<DomainException>(() => policy.ActivateRule(ruleId, LATER));

            Assert.Equal("AP.AAP03", ex.Id);
        }

        // DeactivateRule em regra já inativa lança AP.AAP04.
        [Fact]
        public void DeactivateRule_WhenAlreadyInactive_ShouldThrowDomainException()
        {
            var policy = AutoApprovalPolicyMother.WithAmountThresholdRule(out var ruleId);
            policy.DeactivateRule(ruleId, LATER);

            var ex = Assert.Throws<DomainException>(() => policy.DeactivateRule(ruleId, LATER.AddMinutes(1)));

            Assert.Equal("AP.AAP04", ex.Id);
        }
    }
}
