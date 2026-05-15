namespace AccountsPayable.Domain.Payables.Enumerations;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Channel through which a <see cref="Payable"/> will be paid. Carried on the
/// <c>PayablePaymentRequested</c> event so the sibling <c>PaymentExecution</c> BC knows
/// which adapter to invoke (PIX, boleto, TED) or that no execution is needed (Manual).
/// </summary>
public sealed class PaymentMethod : Enumeration
{
    public static readonly PaymentMethod Pix = new(1, "PIX");
    public static readonly PaymentMethod BankSlip = new(2, "BANK_SLIP");
    public static readonly PaymentMethod Ted = new(3, "TED");
    public static readonly PaymentMethod Manual = new(4, "MANUAL");

    private PaymentMethod(int id, string name) : base(id, name) { }
}
