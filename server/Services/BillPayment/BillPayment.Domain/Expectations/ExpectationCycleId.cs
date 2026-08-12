namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Identidade de um <see cref="ExpectationCycle"/>.</summary>
public readonly record struct ExpectationCycleId(Guid Value) : IEntityId<ExpectationCycleId>
{
    public static ExpectationCycleId New() => new(Guid.CreateVersion7());
    public static ExpectationCycleId From(Guid value) => new(value);
    public static ExpectationCycleId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
