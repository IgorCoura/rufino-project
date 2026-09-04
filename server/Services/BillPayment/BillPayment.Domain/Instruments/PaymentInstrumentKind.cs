namespace BillPayment.Domain.Instruments;

using BillPayment.Domain.SeedWork;

/// <summary>Qual das duas formas de pagar o documento traz.</summary>
public sealed class PaymentInstrumentKind : Enumeration
{
    public static readonly PaymentInstrumentKind Barcode = new(1, "Barcode", PaymentRail.Boleto);
    public static readonly PaymentInstrumentKind PixQr = new(2, "PixQr", PaymentRail.Pix);

    /// <summary>O trilho que este instrumento habilita.</summary>
    public PaymentRail Rail { get; }

    private PaymentInstrumentKind(int id, string name, PaymentRail rail) : base(id, name)
        => Rail = rail;
}
