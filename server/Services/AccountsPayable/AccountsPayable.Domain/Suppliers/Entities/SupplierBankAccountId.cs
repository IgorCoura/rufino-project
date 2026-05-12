namespace AccountsPayable.Domain.Suppliers.Entities;

using AccountsPayable.Domain.SeedWork;

public readonly record struct SupplierBankAccountId(Guid Value) : IEntityId<SupplierBankAccountId>
{
    public static SupplierBankAccountId New() => new(Guid.NewGuid());
    public static SupplierBankAccountId From(Guid value) => new(value);
    public static SupplierBankAccountId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
