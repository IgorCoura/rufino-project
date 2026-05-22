namespace AccountsPayable.Domain.Payables.ValueObjects;

using System.Text;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Código de barras do boleto bancário (44 dígitos). Estrutura padrão Febraban:
/// posições 1-3 (banco), 4 (moeda), 5 (DV mod-11 geral), 6-9 (fator de vencimento),
/// 10-19 (valor), 20-44 (campo livre do banco).
/// <para>
/// Aceita raw input com espaços/pontos; armazena somente dígitos. Valida o dígito verificador
/// geral (posição 5) usando mod-11 com pesos cíclicos 2-9. Bank code "000" é rejeitado
/// (faixa válida é 1-9).
/// </para>
/// </summary>
public sealed class BarcodeDigits : ValueObject
{
    public const int LENGTH = 44;
    public const int DV_POSITION = 4; // 0-based: o 5º caractere

    public string Value { get; }

    public BarcodeDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw BarcodeDigitsErrors.Empty();

        var trimmed = value.Trim();
        if (!trimmed.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '.' || c == '-'))
            throw BarcodeDigitsErrors.NonNumeric();

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length != LENGTH)
            throw BarcodeDigitsErrors.InvalidLength(digits.Length, LENGTH);

        var bankCode = digits[..3];
        if (bankCode == "000")
            throw BarcodeDigitsErrors.InvalidBankCode(bankCode);

        var providedDv = digits[DV_POSITION] - '0';
        var expectedDv = ComputeDvMod11(digits);
        if (providedDv != expectedDv)
            throw BarcodeDigitsErrors.InvalidDigitVerifier(providedDv, expectedDv);

        Value = digits;
    }

    // DV mod-11 do boleto: pesos cíclicos 2..9 da direita pra esquerda sobre os 43 dígitos
    // (excluindo o próprio DV na posição 5). Se resto = 0/1/10 → DV = 1, conforme Febraban.
    private static int ComputeDvMod11(string fullBarcode)
    {
        var sb = new StringBuilder(43);
        sb.Append(fullBarcode.AsSpan(0, DV_POSITION));
        sb.Append(fullBarcode.AsSpan(DV_POSITION + 1));
        var without = sb.ToString();

        var sum = 0;
        var weight = 2;
        for (var i = without.Length - 1; i >= 0; i--)
        {
            sum += (without[i] - '0') * weight;
            weight++;
            if (weight > 9) weight = 2;
        }

        var remainder = sum % 11;
        var dv = 11 - remainder;
        if (dv == 0 || dv == 10 || dv == 11) dv = 1;
        return dv;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Deriva a <see cref="DigitableLine"/> (47 dígitos) a partir deste código de barras. A linha
    /// digitável reorganiza pedaços do barcode em 5 campos e adiciona 3 DVs mod-10 nos campos 1-3:
    /// <list type="bullet">
    /// <item>Campo 1 (10 dígitos): bank+moeda (pos 1-4) + 5 dígitos do campo livre (pos 20-24) + DV1.</item>
    /// <item>Campo 2 (11 dígitos): 10 dígitos do campo livre (pos 25-34) + DV2.</item>
    /// <item>Campo 3 (11 dígitos): 10 dígitos do campo livre (pos 35-44) + DV3.</item>
    /// <item>Campo 4 (1 dígito): DV geral do barcode (pos 5).</item>
    /// <item>Campo 5 (14 dígitos): fator de vencimento + valor (pos 6-19).</item>
    /// </list>
    /// Total: 10+11+11+1+14 = 47.
    /// </summary>
    public DigitableLine ToDigitableLine()
    {
        var s = Value;
        var campo1Base = s[..4] + s[19..24];                 // 4 + 5 = 9
        var campo1 = campo1Base + ComputeMod10(campo1Base); // 10
        var campo2Base = s[24..34];                          // 10
        var campo2 = campo2Base + ComputeMod10(campo2Base); // 11
        var campo3Base = s[34..44];                          // 10
        var campo3 = campo3Base + ComputeMod10(campo3Base); // 11
        var campo4 = s.Substring(DV_POSITION, 1);            // 1
        var campo5 = s[5..19];                               // 14
        return new DigitableLine(campo1 + campo2 + campo3 + campo4 + campo5);
    }

    // Mod-10 do boleto: pesos 2,1,2,1... da direita pra esquerda; se produto ≥ 10, soma dos dígitos.
    // DV = 10 - (sum % 10); se DV = 10, DV = 0.
    private static int ComputeMod10(string s)
    {
        var sum = 0;
        var weight = 2;
        for (var i = s.Length - 1; i >= 0; i--)
        {
            var product = (s[i] - '0') * weight;
            sum += product >= 10 ? product / 10 + product % 10 : product;
            weight = weight == 2 ? 1 : 2;
        }
        var mod = sum % 10;
        return mod == 0 ? 0 : 10 - mod;
    }
}
