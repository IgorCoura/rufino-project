namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.SeedWork;

public readonly record struct CommitmentId(Guid Value) : IEntityId<CommitmentId>
{
    public static CommitmentId New() => new(Guid.CreateVersion7());
    public static CommitmentId From(Guid value) => new(value);
    public static CommitmentId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
