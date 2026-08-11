namespace BillPayment.Domain.Instruments;

using System.Globalization;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A linha digitável de um documento de cobrança, já validada pelos dígitos verificadores e
/// convertida no código de barras de 44 posições.
/// </summary>
/// <remarks>
/// <para>
/// Este VO é a fonte determinística do BC: quem decide valor, banco e vencimento é ele, não
/// a saída de IA nem o texto do PDF. Construir uma instância <strong>é</strong> a prova de
/// que a sequência fecha os DVs — não existe <c>DigitableLine</c> inválida.
/// </para>
/// <para>
/// <strong>O banco sai daqui, não da consulta oficial</strong> (ver `03-bill-validation.md`,
/// check 6). Em cobrança as posições 1–3 do código de barras são o COMPE do banco liquidante,
/// e o DV geral cobre essas posições — alterar o banco quebra o código de barras. É a fonte
/// mais confiável que existe para o destino do dinheiro, e não depende de provedor nenhum.
/// </para>
/// </remarks>
public sealed class DigitableLine : ValueObject
{
    private const int BARCODE_LENGTH = 44;
    private const string UNASSIGNED_BANK = "000";

    /// <summary>Fator 1 = 08/10/1997, o início da contagem original.</summary>
    private static readonly DateTime EPOCH_1997 = new(1997, 10, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Fator 1000 = 22/02/2025, quando a contagem deu a volta.</summary>
    private static readonly DateTime EPOCH_2025 = new(2025, 2, 22, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Só os dígitos, sem pontos nem espaços — 47 em cobrança, 48 em arrecadação.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Sempre 44 posições, reconstruído a partir da linha e validado pelo DV geral.</summary>
    public string Barcode { get; private set; } = string.Empty;

    public BillKind Kind { get; private set; } = default!;

    /// <summary>Sempre em BRL: o layout FEBRABAN não representa outra moeda em uso.</summary>
    public Money Amount { get; private set; } = default!;

    /// <summary>
    /// Nulo quando o fator de vencimento é zero — comum em arrecadação, onde a data real
    /// vive no corpo do documento e não na linha digitável.
    /// </summary>
    public DateTime? DueDate { get; private set; }

    private string? _bankCode;

    private DigitableLine() { }

    /// <summary>
    /// O COMPE do banco liquidante. <strong>Consulte <see cref="BillKind.CarriesBankCode"/>
    /// antes</strong>: arrecadação não tem esse campo e a chamada lança BLP.DGL06.
    /// </summary>
    public BankCode BankCode => Kind.CarriesBankCode
        ? new BankCode(_bankCode!)
        : throw DigitableLineErrors.BankCodeNotAvailable(Kind.Name);

    /// <summary>
    /// Analisa a linha digitável e prova os dígitos verificadores. Aceita a entrada como o
    /// usuário digita — com pontos, espaços e hifens.
    /// </summary>
    /// <param name="today">
    /// Referência para desambiguar o fator de vencimento. Recebida por parâmetro porque o
    /// domínio não lê o relógio: o fator tem 4 dígitos e já deu a volta uma vez, então 1493
    /// é 08/11/2001 ou 30/06/2026 conforme a base — escolhemos o candidato mais próximo
    /// desta data, critério que se corrige sozinho nos ciclos futuros.
    /// </param>
    public static DigitableLine Parse(string value, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw DigitableLineErrors.Required();

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == BillKind.Utility.DigitableLineLength && digits[0] == '8')
            return ParseUtility(digits);
        if (digits.Length == BillKind.BankSlip.DigitableLineLength)
            return ParseBankSlip(digits, today);

        throw DigitableLineErrors.InvalidLength(digits.Length);
    }

    /// <summary>
    /// Reconstrói a linha digitável a partir do código de barras de 44 posições e a analisa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Existe porque o código de barras <strong>impresso</strong> é às vezes a única fonte
    /// legível: em documento digitalizado não há camada de texto, e o que o leitor consegue
    /// decodificar é a barra (Interleaved 2 of 5), não a linha.
    /// </para>
    /// <para>
    /// <strong>Reconstrói e delega ao <see cref="Parse"/></strong> em vez de montar o VO
    /// direto — assim os DVs, o banco não atribuído e o rollover de vencimento continuam sendo
    /// provados num lugar só. Um caminho de construção que pulasse essas checagens seria uma
    /// porta dos fundos para dentro do núcleo determinístico do BC.
    /// </para>
    /// <para>
    /// Em arrecadação o DV de bloco admite mais de um valor aceitável, então a linha
    /// reconstruída pode diferir da impressa em um dígito. Isso <strong>não</strong> afeta a
    /// deduplicação: a chave natural do instrumento vem do <see cref="Barcode"/>, que é idêntico.
    /// </para>
    /// </remarks>
    public static DigitableLine FromBarcode(string barcode, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw DigitableLineErrors.Required();

        var digits = new string(barcode.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length != BARCODE_LENGTH)
            throw DigitableLineErrors.InvalidLength(digits.Length);

        var line = digits[0] == '8' ? UtilityLineFrom(digits) : BankSlipLineFrom(digits);

        return Parse(line, today);
    }

    /// <summary>
    /// Desembaralha o código de barras de cobrança de volta para os cinco campos digitáveis.
    /// É o inverso exato do embaralhamento feito em <c>ParseBankSlip</c>.
    /// </summary>
    private static string BankSlipLineFrom(string barcode)
    {
        var field1 = string.Concat(barcode[..4], barcode[19..24]);
        var field2 = barcode[24..34];
        var field3 = barcode[34..44];

        return string.Concat(
            field1, Mod10(field1).ToString(CultureInfo.InvariantCulture),
            field2, Mod10(field2).ToString(CultureInfo.InvariantCulture),
            field3, Mod10(field3).ToString(CultureInfo.InvariantCulture),
            barcode[4],       // DV geral
            barcode[5..19]);  // fator de vencimento (4) + valor (10)
    }

    /// <summary>
    /// Arrecadação não embaralha: são quatro blocos de 11 na ordem, cada um ganhando seu DV.
    /// </summary>
    private static string UtilityLineFrom(string barcode)
    {
        // O identificador de valor decide o algoritmo do DV de todos os blocos — mesma regra
        // que ParseUtility aplica na direção contrária.
        var usesMod10 = barcode[2] is '6' or '7';
        var line = new System.Text.StringBuilder(BillKind.Utility.DigitableLineLength);

        for (var block = 0; block < 4; block++)
        {
            var digits = barcode.Substring(block * 11, 11);
            var checkDigit = usesMod10 ? Mod10(digits) : Mod11UtilityCandidates(digits).Min();

            line.Append(digits).Append(checkDigit.ToString(CultureInfo.InvariantCulture));
        }

        return line.ToString();
    }

    /// <summary>Tenta analisar sem lançar. Para varredura de texto, onde a maioria dos candidatos falha.</summary>
    public static bool TryParse(string value, DateTime today, out DigitableLine? line)
    {
        try
        {
            line = Parse(value, today);
            return true;
        }
        catch (DomainException)
        {
            line = null;
            return false;
        }
    }

    // ---------- cobrança bancária ----------

    private static DigitableLine ParseBankSlip(string line, DateTime today)
    {
        // Três campos com DV módulo 10 próprio; o quarto dígito solto é o DV geral.
        EnsureFieldCheckDigit(1, line[..9], line[9]);
        EnsureFieldCheckDigit(2, line.Substring(10, 10), line[20]);
        EnsureFieldCheckDigit(3, line.Substring(21, 10), line[31]);

        var generalCheckDigit = line[32];

        // O código de barras não é um recorte da linha: os campos foram embaralhados para
        // caber em blocos digitáveis, e remontá-lo é o que permite conferir o DV geral.
        var barcode = string.Concat(
            line[..4],                  // banco (3) + moeda (1)
            generalCheckDigit,          // DV geral
            line[33..],                 // fator de vencimento (4) + valor (10)
            line.Substring(4, 5),       // resto do campo 1
            line.Substring(10, 10),     // campo 2
            line.Substring(21, 10));    // campo 3

        if (barcode.Length != BARCODE_LENGTH)
            throw DigitableLineErrors.InvalidLength(line.Length);

        // Sem a posição do próprio DV — é sobre os outros 43 dígitos que o módulo 11 corre.
        if (Mod11BankSlip(string.Concat(barcode[..4], barcode[5..])) != generalCheckDigit - '0')
            throw DigitableLineErrors.InvalidGeneralCheckDigit();

        var bankCode = barcode[..3];
        if (string.Equals(bankCode, UNASSIGNED_BANK, StringComparison.Ordinal))
            throw DigitableLineErrors.UnassignedBank();

        return new DigitableLine
        {
            Value = line,
            Barcode = barcode,
            Kind = BillKind.BankSlip,
            _bankCode = bankCode,
            Amount = AmountOf(barcode.Substring(9, 10)),
            DueDate = DueDateOf(int.Parse(barcode.AsSpan(5, 4), CultureInfo.InvariantCulture), today),
        };
    }

    // ---------- arrecadação ----------

    private static DigitableLine ParseUtility(string line)
    {
        // Quatro blocos de 11 dígitos, cada um seguido do seu DV. O código de barras é a
        // concatenação dos blocos — aqui, ao contrário da cobrança, sem embaralhamento.
        var barcode = string.Concat(
            line[..11], line.Substring(12, 11), line.Substring(24, 11), line.Substring(36, 11));

        // O identificador de valor decide o algoritmo do DV de todos os blocos.
        var usesMod10 = barcode[2] is '6' or '7';

        for (var block = 0; block < 4; block++)
        {
            var digits = line.Substring(block * 12, 11);
            var checkDigit = line[(block * 12) + 11] - '0';

            var valid = usesMod10
                ? Mod10(digits) == checkDigit
                : Mod11UtilityCandidates(digits).Contains(checkDigit);

            if (!valid)
                throw DigitableLineErrors.InvalidFieldCheckDigit(block + 1);
        }

        return new DigitableLine
        {
            Value = line,
            Barcode = barcode,
            Kind = BillKind.Utility,
            _bankCode = null,
            Amount = AmountOf(barcode.Substring(4, 11)),

            // Arrecadação não carrega fator de vencimento: a data vive no corpo do documento.
            DueDate = null,
        };
    }

    // ---------- dígitos verificadores ----------

    private static void EnsureFieldCheckDigit(int fieldNumber, string digits, char expected)
    {
        if (Mod10(digits) != expected - '0')
            throw DigitableLineErrors.InvalidFieldCheckDigit(fieldNumber);
    }

    private static int Mod10(string digits)
    {
        var total = 0;
        var multiplier = 2;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var product = (digits[i] - '0') * multiplier;
            if (product > 9)
                product = (product / 10) + (product % 10);

            total += product;
            multiplier = multiplier == 2 ? 1 : 2;
        }

        return (10 - (total % 10)) % 10;
    }

    private static int SumWeighted(string digits)
    {
        var total = 0;
        var weight = 2;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            total += (digits[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        return total;
    }

    private static int Mod11BankSlip(string digits)
    {
        var remainder = 11 - (SumWeighted(digits) % 11);
        return remainder is 0 or 10 or 11 ? 1 : remainder;
    }

    /// <summary>
    /// A especificação de arrecadação tem implementações divergentes em campo para o resto
    /// 0 e 1. Aceitamos as variantes conhecidas em vez de recusar boleto legítimo — recusar
    /// aqui reprovaria documento real por diferença de interpretação do emissor.
    /// </summary>
    private static HashSet<int> Mod11UtilityCandidates(string digits)
    {
        var remainder = SumWeighted(digits) % 11;
        var candidate = 11 - remainder;

        return [candidate > 9 ? 0 : candidate, candidate > 9 ? 1 : candidate, remainder <= 1 ? 0 : candidate];
    }

    // ---------- campos ----------

    private static Money AmountOf(string digits)
        => new(decimal.Parse(digits, CultureInfo.InvariantCulture) / 100m, Currency.BRL);

    private static DateTime? DueDateOf(int factor, DateTime today)
    {
        if (factor == 0)
            return null;

        var candidates = new List<DateTime> { EPOCH_1997.AddDays(factor) };
        if (factor >= 1000)
            candidates.Add(EPOCH_2025.AddDays(factor - 1000));

        return candidates.MinBy(c => Math.Abs((c - today).Ticks));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Barcode;
    }
}
