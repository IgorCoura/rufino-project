namespace AccountsPayable.Domain.Suppliers.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class SupplierStatus : Enumeration
{
    public static readonly SupplierStatus Active = new(1, "ACTIVE");
    public static readonly SupplierStatus Inactive = new(2, "INACTIVE");
    public static readonly SupplierStatus Blocked = new(3, "BLOCKED");

    private SupplierStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(SupplierStatus target) =>
        (Id, target.Id) switch
        {
            (1, 2) => true, // Active -> Inactive
            (1, 3) => true, // Active -> Blocked
            (2, 1) => true, // Inactive -> Active
            (3, 1) => true, // Blocked -> Active
            _ => false
        };
}
