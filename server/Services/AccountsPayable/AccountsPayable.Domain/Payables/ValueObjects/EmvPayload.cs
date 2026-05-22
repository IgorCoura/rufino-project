namespace AccountsPayable.Domain.Payables.ValueObjects;

using System.Text;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// PIX BR Code (EMV-padronizado pelo BACEN). Cobre os três sub-tipos unificados em <c>DynamicPix</c>:
/// boleto-PIX, copia-e-cola e chave temporária — todos compartilham a mesma estrutura EMV.
/// <para>
/// Valida: comprimento mínimo/máximo, prefixo obrigatório do <c>Payload Format Indicator</c> (tag 00,
/// valor "01"), e CRC16-CCITT-FALSE (poly 0x1021, init 0xFFFF) sobre todo o payload exceto os 4
/// caracteres finais, comparado com os 4 chars finais em hexadecimal uppercase.
/// </para>
/// </summary>
public sealed class EmvPayload : ValueObject
{
    public const int MIN_LENGTH = 30;
    public const int MAX_LENGTH = 512;
    public const string MANDATORY_PREFIX = "000201";

    public string Value { get; }

    public EmvPayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw EmvPayloadErrors.Empty();

        var trimmed = value.Trim();

        if (trimmed.Length < MIN_LENGTH)
            throw EmvPayloadErrors.TooShort(trimmed.Length, MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw EmvPayloadErrors.TooLong(trimmed.Length, MAX_LENGTH);
        if (!trimmed.StartsWith(MANDATORY_PREFIX, StringComparison.Ordinal))
            throw EmvPayloadErrors.InvalidPrefix();

        var withoutCrc = trimmed[..^4];
        var providedCrc = trimmed[^4..].ToUpperInvariant();
        var computedCrc = ComputeCrc16(withoutCrc);
        if (!providedCrc.Equals(computedCrc, StringComparison.Ordinal))
            throw EmvPayloadErrors.InvalidCrc16(providedCrc, computedCrc);

        Value = trimmed;
    }

    // CRC16-CCITT-FALSE: poly 0x1021, init 0xFFFF, sem reflection, sem XOR-out. Padrão EMV/BCB.
    private static string ComputeCrc16(string data)
    {
        ushort crc = 0xFFFF;
        var bytes = Encoding.ASCII.GetBytes(data);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc.ToString("X4");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
