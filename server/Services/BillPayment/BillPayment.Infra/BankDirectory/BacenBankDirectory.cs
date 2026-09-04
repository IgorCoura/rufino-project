namespace BillPayment.Infra.BankDirectory;

using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Lê a relação de participantes do STR publicada pelo Banco Central a partir de um snapshot
/// embutido no assembly. Atualize com <c>tools/fetch-bacen-participants.js</c>.
/// </summary>
/// <remarks>
/// <strong>Snapshot embutido, não consulta ao vivo.</strong> Esta tabela decide um check que
/// autoriza pagamento; buscá-la em tempo de validação faria uma indisponibilidade do
/// bcb.gov.br virar bloqueio de pagamento. A tabela muda algumas vezes por ano, e um arquivo
/// versionado deixa a mudança auditável no histórico do repositório.
/// <para>
/// Registrado como <strong>singleton</strong>: o arquivo é lido uma vez e as tabelas são
/// imutáveis a partir daí.
/// </para>
/// </remarks>
internal sealed class BacenBankDirectory : IBankDirectory
{
    private const string RESOURCE_SUFFIX = "BankDirectory.bacen-participants.csv";
    private const int ISPB_LENGTH = 8;

    private readonly FrozenDictionary<string, BankEntry> _byCompe;
    private readonly FrozenDictionary<string, string> _compeByIspb;

    public BacenBankDirectory()
    {
        var entries = ReadSnapshot();

        _byCompe = entries.ToFrozenDictionary(e => e.Compe, StringComparer.Ordinal);
        _compeByIspb = entries
            .GroupBy(e => e.Ispb, StringComparer.Ordinal)
            .ToFrozenDictionary(g => g.Key, g => g.First().Compe, StringComparer.Ordinal);
    }

    public bool IsKnown(BankCode bankCode)
        => bankCode is not null && _byCompe.ContainsKey(bankCode.Value);

    public bool ParticipatesInCompe(BankCode bankCode)
        => bankCode is not null
        && _byCompe.TryGetValue(bankCode.Value, out var entry)
        && entry.ParticipatesInCompe;

    public string? NameOf(BankCode bankCode)
        => bankCode is not null && _byCompe.TryGetValue(bankCode.Value, out var entry)
            ? entry.Name
            : null;

    public BankCode? FromIspb(string ispb)
    {
        if (string.IsNullOrWhiteSpace(ispb))
            return null;

        // O ISPB é numérico de 8 posições e circula com zeros à esquerda comidos.
        var digits = new string(ispb.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0 || digits.Length > ISPB_LENGTH)
            return null;

        return _compeByIspb.TryGetValue(digits.PadLeft(ISPB_LENGTH, '0'), out var compe)
            ? new BankCode(compe)
            : null;
    }

    private static List<BankEntry> ReadSnapshot()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(RESOURCE_SUFFIX, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Snapshot do Bacen não encontrado no assembly ({RESOURCE_SUFFIX}). "
                + "Rode tools/fetch-bacen-participants.js e confira o EmbeddedResource no csproj.");

        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        var entries = new List<BankEntry>();
        _ = reader.ReadLine(); // cabeçalho

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
                continue;

            // O nome é a última coluna e já vem sem vírgula, gravado assim pelo gerador.
            var cells = line.Split(',', 4);
            if (cells.Length != 4)
                throw new InvalidOperationException($"Linha malformada no snapshot do Bacen: {cells.Length} colunas.");

            entries.Add(new BankEntry(
                Ispb: cells[0],
                Compe: cells[1],
                ParticipatesInCompe: cells[2] == "1",
                Name: cells[3]));
        }

        if (entries.Count == 0)
            throw new InvalidOperationException("Snapshot do Bacen está vazio.");

        return entries;
    }

    private sealed record BankEntry(string Ispb, string Compe, bool ParticipatesInCompe, string Name);
}
