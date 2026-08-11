namespace BillPayment.Domain.Instruments;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Uma forma concreta de pagar o documento: código de barras ou QR Pix. VO discriminado —
/// um <c>Bill</c> pode carregar os dois e escolhe o trilho entre eles.
/// </summary>
public sealed class PaymentInstrument : ValueObject
{
    /// <summary>Prefixo da chave natural, para as duas famílias nunca colidirem entre si.</summary>
    private const string BARCODE_KEY_PREFIX = "bc:";
    private const string PIX_KEY_PREFIX = "pix:";

    public PaymentInstrumentKind Kind { get; private set; } = default!;

    private DigitableLine? _digitableLine;
    private PixPayload? _pixPayload;

    /// <summary>
    /// Chave estável do instrumento, usada na deduplicação global. Linha digitável para
    /// boleto; hash do payload para Pix — o payload é longo demais para virar índice e não
    /// deve ficar legível em log de conflito.
    /// </summary>
    public string NaturalKey { get; private set; } = string.Empty;

    /// <summary>
    /// Se a chave natural identifica <strong>um compromisso</strong> ou apenas um meio de
    /// receber. <c>false</c> desliga este instrumento da deduplicação global.
    /// </summary>
    /// <remarks>
    /// Boleto é sempre de uso único: a linha digitável nasce de um título específico.
    /// <strong>QR Pix estático não é</strong> — o mesmo payload é reutilizado indefinidamente,
    /// e é comum um fornecedor mandar todo mês a conta com o mesmo QR. Deduplicar por ele
    /// bloquearia a conta de fevereiro porque a de janeiro existiu. Só o QR dinâmico, que
    /// nasce de uma cobrança específica, é chave confiável.
    /// <para>
    /// Por isso a invariante de unicidade do <c>Bill</c> não se apoia só na chave de
    /// instrumento: ela também compara (beneficiário, valor, vencimento).
    /// </para>
    /// </remarks>
    public bool IsSingleUse { get; private set; }

    /// <summary>Valor que o instrumento declara. Nulo em QR Pix sem valor fixo.</summary>
    public Money? DeclaredAmount { get; private set; }

    private PaymentInstrument() { }

    public static PaymentInstrument FromBarcode(DigitableLine digitableLine)
    {
        if (digitableLine is null)
            throw PaymentInstrumentErrors.DigitableLineRequired();

        return new PaymentInstrument
        {
            Kind = PaymentInstrumentKind.Barcode,
            _digitableLine = digitableLine,
            NaturalKey = BARCODE_KEY_PREFIX + digitableLine.Barcode,
            IsSingleUse = true,
            DeclaredAmount = digitableLine.Amount,
        };
    }

    public static PaymentInstrument FromPixQr(PixPayload payload)
    {
        if (payload is null)
            throw PaymentInstrumentErrors.PixPayloadRequired();

        return new PaymentInstrument
        {
            Kind = PaymentInstrumentKind.PixQr,
            _pixPayload = payload,
            NaturalKey = PIX_KEY_PREFIX + Fingerprint(payload.Payload),

            // Só o QR dinâmico nasce de uma cobrança específica; o estático é reutilizável.
            IsSingleUse = payload.IsDynamic,
            DeclaredAmount = payload.Amount,
        };
    }

    /// <summary>Consulte <see cref="Kind"/> antes — acesso ao tipo errado lança BLP.INS03.</summary>
    public DigitableLine DigitableLine => _digitableLine
        ?? throw PaymentInstrumentErrors.WrongKindAccess("linha digitável", Kind.Name);

    /// <summary>Consulte <see cref="Kind"/> antes — acesso ao tipo errado lança BLP.INS03.</summary>
    public PixPayload PixPayload => _pixPayload
        ?? throw PaymentInstrumentErrors.WrongKindAccess("payload Pix", Kind.Name);

    private static string Fingerprint(string payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))
            .ToLower(CultureInfo.InvariantCulture);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NaturalKey;
    }
}
