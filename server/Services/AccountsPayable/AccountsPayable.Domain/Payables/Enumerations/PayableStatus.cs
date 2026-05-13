namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class PayableStatus : Enumeration
{
    public static readonly PayableStatus Draft = new(1, "DRAFT");
    public static readonly PayableStatus Scheduled = new(2, "SCHEDULED");
    public static readonly PayableStatus Paid = new(3, "PAID");
    public static readonly PayableStatus Cancelled = new(4, "CANCELLED");
    public static readonly PayableStatus AwaitingApproval = new(5, "AWAITING_APPROVAL");
    public static readonly PayableStatus Approved = new(6, "APPROVED");
    public static readonly PayableStatus Rejected = new(7, "REJECTED");

    private PayableStatus(int id, string name) : base(id, name) { }

    public bool CanTransitionTo(PayableStatus target) =>
        (Id, target.Id) switch
        {
            (1, 2) => true, // Draft -> Scheduled         (sem threshold OU ≤ threshold)
            (1, 3) => true, // Draft -> Paid              (pagamento direto, sem agendar)
            (1, 4) => true, // Draft -> Cancelled
            (1, 5) => true, // Draft -> AwaitingApproval  (RequestApproval)
            (2, 3) => true, // Scheduled -> Paid
            (2, 4) => true, // Scheduled -> Cancelled
            (5, 4) => true, // AwaitingApproval -> Cancelled
            (5, 6) => true, // AwaitingApproval -> Approved
            (5, 7) => true, // AwaitingApproval -> Rejected
            (6, 2) => true, // Approved -> Scheduled
            (6, 3) => true, // Approved -> Paid
            (6, 4) => true, // Approved -> Cancelled
            _ => false,     // Paid, Cancelled, Rejected são terminais
        };
}
