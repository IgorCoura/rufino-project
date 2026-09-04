namespace BillPayment.UnitTests.Instruments;

using BillPayment.Domain.Instruments;

/// <summary>
/// Instrumentos sintéticos com DVs e CRC corretos, compartilhados pelos testes que precisam
/// de um instrumento válido sem que ele seja o assunto do teste.
/// </summary>
/// <remarks>
/// Linha digitável e BR Code reais são instrumentos de pagamento e não entram no repositório.
/// </remarks>
internal static class InstrumentSamples
{
    /// <summary>Referência fixa para desambiguar o fator de vencimento — o domínio não lê relógio.</summary>
    public static readonly DateTime Today = new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    public const string BankSlipLine341 = "34191234546789012345767890123457314880000061507";
    public const string BankSlipLine033 = "03399876534321098765743210987657414930000140980";
    public const string UtilityLine = "826600000010224812345672890123456786901234567898";

    public const string StaticPixWithAmount =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    public const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    public static PaymentInstrument Barcode(string line = BankSlipLine341)
        => PaymentInstrument.FromBarcode(DigitableLine.Parse(line, Today));

    public static PaymentInstrument UtilityBarcode()
        => PaymentInstrument.FromBarcode(DigitableLine.Parse(UtilityLine, Today));

    public static PaymentInstrument StaticPix()
        => PaymentInstrument.FromPixQr(PixPayload.Parse(StaticPixWithAmount));

    public static PaymentInstrument DynamicPixQr()
        => PaymentInstrument.FromPixQr(PixPayload.Parse(DynamicPix));
}
