namespace BillPayment.Domain.Instruments;

using System.Globalization;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O BR Code de um QR Pix (padrão EMV MPM), já validado pelo CRC e decomposto nos campos que
/// a verificação usa.
/// </summary>
/// <remarks>
/// <para>
/// Assim como <see cref="DigitableLine"/>, este VO é fonte determinística: construir a
/// instância <strong>é</strong> a prova de que o CRC fecha. Um QR copiado pela metade — o
/// erro mais comum de "copia e cola" — não produz instância.
/// </para>
/// <para>
/// <strong>O que ele não sabe:</strong> o CPF/CNPJ do recebedor. O BR Code carrega chave e
/// nome, não documento — o documento só vem da consulta ao PSP. Por isso o check de
/// consistência entre QR e código de barras precisa da consulta dos dois lados.
/// </para>
/// </remarks>
public sealed class PixPayload : ValueObject
{
    /// <summary>Identificador do arranjo Pix dentro do template de conta do comerciante.</summary>
    public const string PIX_GUI = "br.gov.bcb.pix";

    private const int CRC_LENGTH = 4;
    private const string CRC_FIELD_ID = "63";
    private const string BRL_CURRENCY_CODE = "986";
    private const string DYNAMIC_INITIATION_METHOD = "12";

    // Faixa reservada pelo EMV aos templates de conta do comerciante.
    private const int MERCHANT_ACCOUNT_FIRST = 26;
    private const int MERCHANT_ACCOUNT_LAST = 51;

    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// QR dinâmico é de uso único e carrega URL em vez de valor; estático é reutilizável.
    /// Um QR estático sem valor aceita qualquer quantia — o check de valor sai <c>Skipped</c>.
    /// </summary>
    public bool IsDynamic { get; private set; }

    /// <summary>A chave Pix do recebedor. Nula em QR dinâmico, que aponta para uma URL.</summary>
    public string? PixKey { get; private set; }

    /// <summary>URL do payload dinâmico, de onde o PSP lê os dados reais da cobrança.</summary>
    public string? Url { get; private set; }

    /// <summary>Nulo quando o QR não fixa valor — caso comum em QR estático.</summary>
    public Money? Amount { get; private set; }

    public string? MerchantName { get; private set; }
    public string? MerchantCity { get; private set; }

    /// <summary>Identificador da transação, usado para conciliação.</summary>
    public string? TransactionId { get; private set; }

    private PixPayload() { }

    public static PixPayload Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw PixPayloadErrors.Required();

        var text = payload.Trim();
        EnsureCrc(text);

        var fields = ReadFields(text);

        if (!fields.ContainsKey("00"))
            throw PixPayloadErrors.MalformedPayload("indicador de formato ausente");

        var merchantAccount = FindPixMerchantAccount(fields)
            ?? throw PixPayloadErrors.NotPix();

        var additionalData = fields.TryGetValue("62", out var additional)
            ? ReadFields(additional)
            : [];

        return new PixPayload
        {
            Payload = text,
            IsDynamic = fields.GetValueOrDefault("01") == DYNAMIC_INITIATION_METHOD,
            PixKey = Blank(merchantAccount.GetValueOrDefault("01")),
            Url = Blank(merchantAccount.GetValueOrDefault("25")),
            Amount = ReadAmount(fields),
            MerchantName = Blank(fields.GetValueOrDefault("59")),
            MerchantCity = Blank(fields.GetValueOrDefault("60")),
            TransactionId = Blank(additionalData.GetValueOrDefault("05")),
        };
    }

    /// <summary>Tenta analisar sem lançar. Para varredura de documento, onde a maioria dos candidatos falha.</summary>
    public static bool TryParse(string payload, out PixPayload? parsed)
    {
        try
        {
            parsed = Parse(payload);
            return true;
        }
        catch (DomainException)
        {
            parsed = null;
            return false;
        }
    }

    /// <summary>
    /// CRC-16/CCITT-FALSE sobre todo o payload até o valor do campo 63 — inclusive o próprio
    /// <c>"6304"</c>. Exposto porque gerar o CRC é o único jeito de escrever fixture válida.
    /// </summary>
    public static string ComputeCrc(string payloadUpToCrcValue)
    {
        ushort crc = 0xFFFF;

        foreach (var b in System.Text.Encoding.UTF8.GetBytes(payloadUpToCrcValue))
        {
            crc ^= (ushort)(b << 8);

            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static void EnsureCrc(string text)
    {
        // O campo 63 é sempre o último e tem tamanho fixo: "6304" + 4 dígitos hexadecimais.
        const int crcFieldLength = 4 + CRC_LENGTH;

        if (text.Length < crcFieldLength)
            throw PixPayloadErrors.MalformedPayload("payload curto demais para conter o CRC");

        var crcHeaderStart = text.Length - crcFieldLength;
        if (!text.AsSpan(crcHeaderStart, 4).SequenceEqual($"{CRC_FIELD_ID}0{CRC_LENGTH}"))
            throw PixPayloadErrors.MalformedPayload("o campo de CRC não fecha o payload");

        var declared = text[^CRC_LENGTH..];
        var computed = ComputeCrc(text[..^CRC_LENGTH]);

        if (!string.Equals(declared, computed, StringComparison.OrdinalIgnoreCase))
            throw PixPayloadErrors.InvalidCrc();
    }

    /// <summary>
    /// Lê a sequência TLV do EMV: dois dígitos de id, dois de tamanho, e o valor. Chave
    /// repetida mantém a primeira ocorrência — o padrão não prevê repetição, e preferir a
    /// última deixaria um campo colado no fim sobrescrever o legítimo.
    /// </summary>
    private static Dictionary<string, string> ReadFields(string text)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var position = 0;

        while (position < text.Length)
        {
            if (position + 4 > text.Length)
                throw PixPayloadErrors.MalformedPayload("campo truncado");

            var id = text.Substring(position, 2);
            if (!int.TryParse(text.AsSpan(position + 2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var length))
                throw PixPayloadErrors.MalformedPayload($"tamanho inválido no campo {id}");

            position += 4;

            if (position + length > text.Length)
                throw PixPayloadErrors.MalformedPayload($"o campo {id} declara mais dados do que existem");

            fields.TryAdd(id, text.Substring(position, length));
            position += length;
        }

        return fields;
    }

    private static Dictionary<string, string>? FindPixMerchantAccount(Dictionary<string, string> fields)
    {
        for (var id = MERCHANT_ACCOUNT_FIRST; id <= MERCHANT_ACCOUNT_LAST; id++)
        {
            if (!fields.TryGetValue(id.ToString("D2", CultureInfo.InvariantCulture), out var template))
                continue;

            Dictionary<string, string> inner;
            try
            {
                inner = ReadFields(template);
            }
            catch (DomainException)
            {
                // Template de outro arranjo pode não seguir TLV; não é motivo para recusar o QR.
                continue;
            }

            if (string.Equals(inner.GetValueOrDefault("00"), PIX_GUI, StringComparison.OrdinalIgnoreCase))
                return inner;
        }

        return null;
    }

    private static Money? ReadAmount(Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("54", out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            throw PixPayloadErrors.MalformedPayload("valor da transação ilegível");

        // Moeda diferente de real com valor fixo não é pagável pelos trilhos deste BC.
        var currency = fields.GetValueOrDefault("53");
        if (currency is not null && !string.Equals(currency, BRL_CURRENCY_CODE, StringComparison.Ordinal))
            throw PixPayloadErrors.MalformedPayload($"moeda {currency} não suportada");

        return new Money(amount, Currency.BRL);
    }

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Payload;
    }
}
