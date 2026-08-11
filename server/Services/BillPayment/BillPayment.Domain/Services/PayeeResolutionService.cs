namespace BillPayment.Domain.Services;

using System.Globalization;
using System.Text;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Como o beneficiário que a consulta oficial devolveu se relaciona com o cadastro do tenant.
/// </summary>
public sealed class PayeeMatchKind : Enumeration
{
    /// <summary>Casou por documento fiscal. O sinal forte — CNPJ não muda de dono.</summary>
    public static readonly PayeeMatchKind ByTaxId = new(1, nameof(ByTaxId));

    /// <summary>
    /// Casou só por nome. É o que resta em arrecadação, onde a consulta não devolve documento
    /// em 100% dos casos medidos. Vale menos: nome é falsificável e varia em grafia.
    /// </summary>
    public static readonly PayeeMatchKind ByName = new(2, nameof(ByName));

    /// <summary>Nenhum cadastro corresponde. Beneficiário novo é rotina, não falha.</summary>
    public static readonly PayeeMatchKind NotFound = new(3, nameof(NotFound));

    /// <summary>
    /// Documento diferente de todos os cadastros, mas nome muito parecido com um deles.
    /// <strong>É o cenário de fraude de boleto</strong> e o que justifica a severidade
    /// bloqueante do check de beneficiário.
    /// </summary>
    public static readonly PayeeMatchKind Lookalike = new(4, nameof(Lookalike));

    private PayeeMatchKind(int id, string name) : base(id, name) { }
}

/// <summary>O que a resolução encontrou. Valor puro — quem muta é o <c>Bill</c>.</summary>
public sealed class PayeeResolution : ValueObject
{
    public PayeeMatchKind Kind { get; private set; } = default!;

    /// <summary>O beneficiário cadastrado, quando houve correspondência.</summary>
    public Payee? Payee { get; private set; }

    /// <summary>Razão social do cadastro parecido, quando <see cref="PayeeMatchKind.Lookalike"/>.</summary>
    public string? LookalikeName { get; private set; }

    private PayeeResolution() { }

    internal static PayeeResolution Matched(Payee payee, PayeeMatchKind kind)
        => new() { Kind = kind, Payee = payee };

    internal static PayeeResolution NotFound() => new() { Kind = PayeeMatchKind.NotFound };

    internal static PayeeResolution Lookalike(Payee similar)
        => new() { Kind = PayeeMatchKind.Lookalike, Payee = similar, LookalikeName = similar.LegalName };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return Payee?.Id;
        yield return LookalikeName;
    }
}

/// <summary>
/// Resolve o beneficiário da consulta oficial para um <c>Payee</c> cadastrado do tenant.
/// </summary>
/// <remarks>
/// <para>
/// A ordem é deliberada: <strong>documento primeiro, nome depois, sósia por último</strong>.
/// Documento é o sinal forte; nome é o que resta quando a consulta não devolve documento; e a
/// checagem de sósia só faz sentido depois de esgotar as duas — ela existe para pegar o boleto
/// que usa o nome de um fornecedor conhecido com o CNPJ de outra pessoa.
/// </para>
/// <para>
/// É serviço, e não método do <c>Payee</c>, porque a pergunta cruza <c>Bill</c> (de onde vem o
/// beneficiário consultado) e o conjunto de <c>Payee</c> do tenant.
/// </para>
/// </remarks>
public static class PayeeResolutionService
{
    /// <summary>
    /// Quão parecidos dois nomes precisam ser para levantar suspeita de sósia. Começa
    /// <strong>alto de propósito</strong> — o risco registrado no roadmap é de falso bloqueio,
    /// e o limiar é para ser calibrado com dados reais.
    /// </summary>
    public const double LOOKALIKE_SIMILARITY_THRESHOLD = 0.85;

    private const int MIN_NAME_LENGTH_FOR_LOOKALIKE = 5;

    public static PayeeResolution Resolve(LookupParty? beneficiary, IReadOnlyCollection<Payee> candidates)
    {
        if (beneficiary is null || candidates is null || candidates.Count == 0)
            return PayeeResolution.NotFound();

        // Com documento na consulta, o documento decide — e só ele.
        if (beneficiary.TaxId is not null)
        {
            var byTaxId = candidates.FirstOrDefault(p => p.TaxId.Equals(beneficiary.TaxId));
            if (byTaxId is not null)
                return PayeeResolution.Matched(byTaxId, PayeeMatchKind.ByTaxId);

            // O documento veio e não casou com cadastro nenhum. Cair para o nome aqui seria o
            // erro grave: nome igual ao de um fornecedor conhecido com CNPJ de outra pessoa é
            // exatamente a fraude de boleto, e vira sósia — nunca confirmação.
            var similar = MostSimilar(beneficiary, candidates);
            return similar is null ? PayeeResolution.NotFound() : PayeeResolution.Lookalike(similar);
        }

        // Sem documento — o caso de 100% da arrecadação — o nome é o que resta. E aqui não há
        // "outro CNPJ com o mesmo nome": há apenas um nome que casou ou não casou.
        var byName = candidates.FirstOrDefault(p => NameMatches(p, beneficiary));
        return byName is null ? PayeeResolution.NotFound() : PayeeResolution.Matched(byName, PayeeMatchKind.ByName);
    }

    /// <summary>
    /// Compara dois nomes ignorando acento, pontuação e caixa. Mais tolerante que o
    /// <c>MatchesName</c> do cadastro <strong>de propósito</strong>: aqui o objetivo é
    /// <em>levantar</em> semelhança suspeita, não confirmar identidade.
    /// </summary>
    public static double Similarity(string? left, string? right)
    {
        var a = NormalizeForComparison(left);
        var b = NormalizeForComparison(right);

        if (a.Length == 0 || b.Length == 0)
            return 0d;
        if (string.Equals(a, b, StringComparison.Ordinal))
            return 1d;

        var distance = EditDistance(a, b);
        return 1d - ((double)distance / Math.Max(a.Length, b.Length));
    }

    private static bool NameMatches(Payee payee, LookupParty beneficiary)
        => (beneficiary.Name is not null && payee.MatchesName(beneficiary.Name))
        || (beneficiary.TradingName is not null && payee.MatchesName(beneficiary.TradingName));

    private static Payee? MostSimilar(LookupParty beneficiary, IReadOnlyCollection<Payee> candidates)
    {
        Payee? best = null;
        var bestScore = LOOKALIKE_SIMILARITY_THRESHOLD;

        foreach (var candidate in candidates)
        {
            foreach (var name in new[] { beneficiary.Name, beneficiary.TradingName })
            {
                if (NormalizeForComparison(name).Length < MIN_NAME_LENGTH_FOR_LOOKALIKE)
                    continue;

                var score = Math.Max(
                    Similarity(name, candidate.LegalName),
                    candidate.Aliases.Count == 0 ? 0d : candidate.Aliases.Max(alias => Similarity(name, alias)));

                if (score >= bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
        }

        return best;
    }

    private static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }

    // Levenshtein com duas linhas: o nome mais longo do cadastro tem centenas de caracteres e
    // a matriz cheia seria alocação desnecessária num caminho que roda por boleto.
    private static int EditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
