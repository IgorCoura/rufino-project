namespace AccountsPayable.UnitTests.SeedWork._TestDoubles;

using AccountsPayable.Domain.SeedWork;

internal sealed class TestStatus : Enumeration
{
    public static readonly TestStatus Draft = new(1, "DRAFT");
    public static readonly TestStatus Active = new(2, "ACTIVE");
    public static readonly TestStatus Closed = new(3, "CLOSED");

    private TestStatus(int id, string name) : base(id, name) { }
}
