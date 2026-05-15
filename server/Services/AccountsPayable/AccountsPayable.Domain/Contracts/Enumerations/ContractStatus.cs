namespace AccountsPayable.Domain.Contracts.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class ContractStatus : Enumeration
{
    public static readonly ContractStatus Draft = new(1, "DRAFT");
    public static readonly ContractStatus Active = new(2, "ACTIVE");
    public static readonly ContractStatus Suspended = new(3, "SUSPENDED");
    public static readonly ContractStatus Terminated = new(4, "TERMINATED");

    private ContractStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(ContractStatus target) =>
        (Id, target.Id) switch
        {
            (1, 2) => true, // Draft -> Active
            (1, 4) => true, // Draft -> Terminated
            (2, 3) => true, // Active -> Suspended
            (2, 4) => true, // Active -> Terminated
            (3, 2) => true, // Suspended -> Active (Resume)
            (3, 4) => true, // Suspended -> Terminated
            _ => false,     // Terminated é terminal
        };
}
