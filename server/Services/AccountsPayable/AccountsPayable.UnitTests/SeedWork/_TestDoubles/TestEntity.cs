namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal sealed class TestEntity : Entity<TestId>
{
    public TestEntity() : base() { }
    public TestEntity(TestId id) : base(id) { }

    public void AssignId(TestId id) => Id = id;
}
