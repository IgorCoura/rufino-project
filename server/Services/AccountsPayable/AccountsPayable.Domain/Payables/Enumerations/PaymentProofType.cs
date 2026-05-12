namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

public sealed class PaymentProofType : Enumeration
{
    public static readonly PaymentProofType Receipt = new(1, "RECEIPT");
    public static readonly PaymentProofType BankSlip = new(2, "BANK_SLIP");
    public static readonly PaymentProofType BankStatement = new(3, "BANK_STATEMENT");
    public static readonly PaymentProofType Other = new(4, "OTHER");

    private PaymentProofType(int id, string name) : base(id, name) { }
}
