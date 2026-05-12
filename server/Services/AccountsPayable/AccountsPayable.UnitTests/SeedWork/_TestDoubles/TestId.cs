namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal readonly record struct TestId(Guid Value) : IEntityId<TestId>
{
    public static TestId New() => new(Guid.NewGuid());
    public static TestId From(Guid value) => new(value);
    public static TestId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
