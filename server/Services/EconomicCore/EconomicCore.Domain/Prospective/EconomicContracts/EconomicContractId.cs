namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicContractId(Guid Value) : IEntityId<EconomicContractId>
{
    public static EconomicContractId New() => new(Guid.CreateVersion7());
    public static EconomicContractId From(Guid value) => new(value);
    public static EconomicContractId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
