namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class PayableStatus : Enumeration
{
    public static readonly PayableStatus Draft = new(1, "DRAFT");
    public static readonly PayableStatus Scheduled = new(2, "SCHEDULED");
    public static readonly PayableStatus Paid = new(3, "PAID");
    public static readonly PayableStatus Cancelled = new(4, "CANCELLED");

    private PayableStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(PayableStatus target) =>
        (Id, target.Id) switch
        {
            (1, 2) => true, // Draft -> Scheduled
            (1, 3) => true, // Draft -> Paid (pode pagar antes de agendar)
            (1, 4) => true, // Draft -> Cancelled
            (2, 3) => true, // Scheduled -> Paid
            (2, 4) => true, // Scheduled -> Cancelled
            _ => false,     // Paid e Cancelled são terminais
        };
}
