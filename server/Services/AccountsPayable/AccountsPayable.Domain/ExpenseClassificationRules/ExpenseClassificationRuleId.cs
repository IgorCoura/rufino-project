namespace AccountsPayable.Domain.ExpenseClassificationRules;

using AccountsPayable.Domain.SeedWork;

public readonly record struct ExpenseClassificationRuleId(Guid Value) : IEntityId<ExpenseClassificationRuleId>
{
    public static ExpenseClassificationRuleId New() => new(Guid.NewGuid());
    public static ExpenseClassificationRuleId From(Guid value) => new(value);
    public static ExpenseClassificationRuleId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
