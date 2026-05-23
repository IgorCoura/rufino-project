namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class ContractStatus : Enumeration
{
    public static readonly ContractStatus Active = new(1, "ACTIVE");
    public static readonly ContractStatus Suspended = new(2, "SUSPENDED");
    public static readonly ContractStatus Terminated = new(3, "TERMINATED");

    private ContractStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(ContractStatus target)
        => (Id, target.Id) switch
        {
            (1, 2) => true, // Active -> Suspended
            (1, 3) => true, // Active -> Terminated
            (2, 1) => true, // Suspended -> Active
            (2, 3) => true, // Suspended -> Terminated
            _ => false,
        };
}
