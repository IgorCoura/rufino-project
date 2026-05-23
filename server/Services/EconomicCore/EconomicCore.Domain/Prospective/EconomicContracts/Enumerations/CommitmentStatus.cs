namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class CommitmentStatus : Enumeration
{
    public static readonly CommitmentStatus Promised = new(1, "PROMISED");
    public static readonly CommitmentStatus Reserved = new(2, "RESERVED");
    public static readonly CommitmentStatus Fulfilled = new(3, "FULFILLED");
    public static readonly CommitmentStatus Expired = new(4, "EXPIRED");
    public static readonly CommitmentStatus Cancelled = new(5, "CANCELLED");

    private CommitmentStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(CommitmentStatus target)
        => (Id, target.Id) switch
        {
            (1, 2) => true, // Promised -> Reserved
            (1, 3) => true, // Promised -> Fulfilled
            (1, 4) => true, // Promised -> Expired
            (1, 5) => true, // Promised -> Cancelled
            (2, 3) => true, // Reserved -> Fulfilled
            (2, 4) => true, // Reserved -> Expired
            (2, 5) => true, // Reserved -> Cancelled
            _ => false,
        };

    public bool IsTerminal => Id == 3 || Id == 4 || Id == 5; // Fulfilled, Expired, Cancelled
}
