namespace AccountsPayable.UnitTests.ExpenseClassificationRules;

using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.ExpenseClassificationRules.Events;
using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.ExpenseClassificationRules.Mothers;

public class ExpenseClassificationRuleTests
{
    private static readonly DateTime FIXED_NOW = ExpenseClassificationRuleMother.DEFAULT_OCCURRED_AT;
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreatingManually
    {
        // CreateManual produz regra Active, sem LearnedFromUserId, e emite ExpenseClassificationRuleCreated com payload completo.
        [Fact]
        public void CreateManual_WithValidInputs_ShouldStartActiveWithoutLearningOrigin()
        {
            var rule = ExpenseClassificationRuleMother.Active(priority: 7);

            Assert.True(rule.IsActive);
            Assert.Equal(7, rule.Priority);
            Assert.Null(rule.LearnedFromUserId);
            var created = Assert.IsType<ExpenseClassificationRuleCreated>(rule.PullDomainEvents().Single());
            Assert.Equal(rule.Id, created.RuleId);
            Assert.Equal(7, created.Priority);
            Assert.True(created.IsActive);
            Assert.Null(created.LearnedFromUserId);
            Assert.Equal(ExpenseClassificationRuleMother.DEFAULT_SUPPLIER, created.MatchSupplierId);
        }

        // CreateManual com priority < 1 lança AP.ECR01.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreateManual_WithNonPositivePriority_ShouldThrowDomainException(int priority)
        {
            var ex = Assert.Throws<DomainException>(() => ExpenseClassificationRuleMother.Active(priority: priority));

            Assert.Equal("AP.ECR01", ex.Id);
        }
    }

    public class WhenLearningFromHistory
    {
        // LearnFromHistory produz regra Active e popula LearnedFromUserId com o usuário que originou o padrão.
        [Fact]
        public void LearnFromHistory_WithValidInputs_ShouldRecordLearningOrigin()
        {
            var rule = ExpenseClassificationRuleMother.Learned();

            Assert.True(rule.IsActive);
            Assert.Equal(ExpenseClassificationRuleMother.DEFAULT_USER, rule.LearnedFromUserId);
            var created = Assert.IsType<ExpenseClassificationRuleCreated>(rule.PullDomainEvents().Single());
            Assert.Equal(ExpenseClassificationRuleMother.DEFAULT_USER, created.LearnedFromUserId);
        }
    }

    public class WhenUpdating
    {
        // Update troca Match/Action/Priority e emite ExpenseClassificationRuleUpdated com os novos valores.
        [Fact]
        public void Update_WithNewValues_ShouldMutateAndEmitEvent()
        {
            var rule = ExpenseClassificationRuleMother.Active(priority: 5);
            rule.PullDomainEvents();
            var newMatch = new ClassificationMatcher(keyword: "energia elétrica");
            var newAction = ExpenseClassificationRuleMother.DefaultAction(autoApprove: true);

            rule.Update(newMatch, newAction, priority: 1, LATER);

            Assert.Equal(1, rule.Priority);
            Assert.Equal("energia elétrica", rule.Match.Keyword);
            Assert.True(rule.Action.AutoApprove);
            var updated = Assert.IsType<ExpenseClassificationRuleUpdated>(rule.PullDomainEvents().Single());
            Assert.Equal(1, updated.Priority);
            Assert.Equal("energia elétrica", updated.MatchKeyword);
            Assert.True(updated.ActionAutoApprove);
        }

        // Update com priority < 1 lança AP.ECR01.
        [Fact]
        public void Update_WithNonPositivePriority_ShouldThrowDomainException()
        {
            var rule = ExpenseClassificationRuleMother.Active();

            var ex = Assert.Throws<DomainException>(() => rule.Update(
                ExpenseClassificationRuleMother.SupplierMatcher(),
                ExpenseClassificationRuleMother.DefaultAction(),
                priority: 0,
                LATER));

            Assert.Equal("AP.ECR01", ex.Id);
        }
    }

    public class WhenActivating
    {
        // Activate em regra inativa muda IsActive=true e emite ExpenseClassificationRuleActivated.
        [Fact]
        public void Activate_FromInactive_ShouldFlipAndEmitEvent()
        {
            var rule = ExpenseClassificationRuleMother.Active();
            rule.Deactivate(LATER);
            rule.PullDomainEvents();

            rule.Activate(LATER.AddMinutes(1));

            Assert.True(rule.IsActive);
            Assert.IsType<ExpenseClassificationRuleActivated>(rule.PullDomainEvents().Single());
        }

        // Activate em regra já ativa lança AP.ECR02 (idempotência protegida).
        [Fact]
        public void Activate_WhenAlreadyActive_ShouldThrowDomainException()
        {
            var rule = ExpenseClassificationRuleMother.Active();

            var ex = Assert.Throws<DomainException>(() => rule.Activate(LATER));

            Assert.Equal("AP.ECR02", ex.Id);
        }
    }

    public class WhenDeactivating
    {
        // Deactivate em regra ativa muda IsActive=false e emite ExpenseClassificationRuleDeactivated.
        [Fact]
        public void Deactivate_FromActive_ShouldFlipAndEmitEvent()
        {
            var rule = ExpenseClassificationRuleMother.Active();
            rule.PullDomainEvents();

            rule.Deactivate(LATER);

            Assert.False(rule.IsActive);
            Assert.IsType<ExpenseClassificationRuleDeactivated>(rule.PullDomainEvents().Single());
        }

        // Deactivate em regra já inativa lança AP.ECR03.
        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldThrowDomainException()
        {
            var rule = ExpenseClassificationRuleMother.Active();
            rule.Deactivate(LATER);

            var ex = Assert.Throws<DomainException>(() => rule.Deactivate(LATER.AddMinutes(1)));

            Assert.Equal("AP.ECR03", ex.Id);
        }
    }
}
