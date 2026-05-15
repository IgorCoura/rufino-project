namespace AccountsPayable.Domain.Contracts;

using AccountsPayable.Domain.SeedWork;

public readonly record struct ContractId(Guid Value) : IEntityId<ContractId>
{
    public static ContractId New() => new(Guid.NewGuid());
    public static ContractId From(Guid value) => new(value);
    public static ContractId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
