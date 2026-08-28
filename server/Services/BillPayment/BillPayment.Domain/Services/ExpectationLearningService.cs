namespace BillPayment.Domain.Services;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>Uma ocorrência histórica de conta paga, reduzida ao que o aprendizado precisa.</summary>
/// <param name="ArrivedOn">Quando o documento entrou no sistema.</param>
/// <param name="DueDate">Quando ele vencia.</param>
/// <param name="SourceId">
/// Por onde ele chegou. Vira o <c>HintSourceId</c> da expectativa — a única coisa capaz de ligar
/// um artefato travado (que não tem beneficiário nem vencimento) à conta que ele seria. Nulo em
/// boleto importado à mão.
/// </param>
public sealed record BillOccurrence(DateOnly ArrivedOn, DateOnly DueDate, CaptureSourceId? SourceId = null);

/// <summary>Por que o histórico de um beneficiário não virou expectativa.</summary>
public sealed class LearningRefusal : Enumeration
{
    /// <summary>Menos de três ocorrências — não dá para afirmar recorrência nenhuma.</summary>
    public static readonly LearningRefusal TooFewOccurrences = new(1, nameof(TooFewOccurrences));

    /// <summary>O espaçamento não bate com recorrência conhecida dentro da tolerância.</summary>
    public static readonly LearningRefusal Irregular = new(2, nameof(Irregular));

    /// <summary>
    /// O histórico mostra <strong>mais de uma conta</strong> do mesmo beneficiário, e não há como
    /// dizer qual é qual — quem separa é o cadastro manual.
    /// </summary>
    public static readonly LearningRefusal MultipleAccounts = new(3, nameof(MultipleAccounts));

    private LearningRefusal(int id, string name) : base(id, name) { }
}

/// <summary>O que o aprendizado concluiu sobre um beneficiário.</summary>
public sealed class LearningProposal : ValueObject
{
    public PayeeId PayeeId { get; }

    /// <summary>Preenchido só quando há proposta.</summary>
    public Recurrence? Recurrence { get; }

    public int ExpectedDueDay { get; }
    public int ObservedLeadDays { get; }
    public int ObservationCount { get; }

    /// <summary>
    /// A competência da ocorrência mais recente — a fase da recorrência. Preenchida só quando há
    /// proposta.
    /// </summary>
    /// <remarks>
    /// Sem ela, uma expectativa bimestral, trimestral ou anual não teria como saber em quais
    /// <em>meses</em> a conta vence, e a varredura abriria um ciclo por mês para todas elas.
    /// </remarks>
    public CompetencePeriod? AnchorCompetence { get; }

    /// <summary>A fonte pela qual a conta mais chega. Preenchida só quando há proposta.</summary>
    public CaptureSourceId? HintSourceId { get; }

    /// <summary>Preenchido só quando NÃO há proposta.</summary>
    public LearningRefusal? Refusal { get; }

    public bool IsProposal => Refusal is null;

    private LearningProposal(
        PayeeId payeeId,
        Recurrence? recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int observationCount,
        CompetencePeriod? anchorCompetence,
        CaptureSourceId? hintSourceId,
        LearningRefusal? refusal)
    {
        PayeeId = payeeId;
        Recurrence = recurrence;
        ExpectedDueDay = expectedDueDay;
        ObservedLeadDays = observedLeadDays;
        ObservationCount = observationCount;
        AnchorCompetence = anchorCompetence;
        HintSourceId = hintSourceId;
        Refusal = refusal;
    }

    internal static LearningProposal Propose(
        PayeeId payeeId,
        Recurrence recurrence,
        int dueDay,
        int leadDays,
        int count,
        CompetencePeriod anchorCompetence,
        CaptureSourceId? hintSourceId)
        => new(payeeId, recurrence, dueDay, leadDays, count, anchorCompetence, hintSourceId, refusal: null);

    internal static LearningProposal Refuse(PayeeId payeeId, LearningRefusal refusal, int count)
        => new(payeeId, recurrence: null, 0, 0, count, anchorCompetence: null, hintSourceId: null, refusal);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PayeeId;
        yield return Recurrence;
        yield return ExpectedDueDay;
        yield return ObservedLeadDays;
        yield return ObservationCount;
        yield return AnchorCompetence;
        yield return HintSourceId;
        yield return Refusal;
    }
}

/// <summary>
/// Propõe expectativas a partir do histórico de boletos de um beneficiário — e recusa quando o
/// histórico não sustenta.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Domain Service porque cruza dois Aggregates</strong> — o histórico de <c>Bill</c> e
/// o <c>Payee</c>. Estático e puro, como os outros do BC: quem carrega os dados é o handler.
/// </para>
/// <para>
/// <strong>Devolve candidata, nunca persiste, e nunca decide sozinho que vai alertar.</strong> A
/// expectativa criada notifica o usuário na hora ("passei a monitorar a conta X"), porque criar
/// em silêncio faria a primeira notícia da existência dela ser um alerta que ninguém pediu.
/// </para>
/// <para>
/// <strong>A recusa por múltiplas contas é a regra que a medição impôs.</strong> Medido em
/// 2026-08-12: 10 dos 20 grupos de beneficiário do arquivo real têm mais de uma conta do mesmo
/// tenant — quatro instalações da EDP, três do DAE. A referência que as separa existe no campo
/// livre do código de barras em arrecadação, mas em posição que muda por emissor; deduzi-la seria
/// adivinhação, e uma expectativa por beneficiário seria cumprida pela primeira conta que
/// chegasse, escondendo as outras. Então o serviço <em>não adivinha</em>: recusa, e o usuário
/// cadastra cada conta com a referência que ele conhece.
/// </para>
/// </remarks>
public static class ExpectationLearningService
{
    /// <summary>Mínimo de ocorrências para afirmar recorrência. Duas descrevem um intervalo só.</summary>
    public const int MIN_OCCURRENCES = 3;

    /// <summary>
    /// Quantas contas do mesmo beneficiário o histórico comporta antes de o aprendizado desistir.
    /// </summary>
    public const int MAX_ACCOUNTS_TO_LEARN = 1;

    /// <summary>
    /// Quanto um intervalo isolado pode se afastar da recorrência escolhida antes de o histórico
    /// ser considerado irregular. Mais folgado que a tolerância da própria recorrência porque
    /// aqui já se sabe qual ela é — o que se mede é a dispersão, não a identificação.
    /// </summary>
    private const int IRREGULARITY_TOLERANCE_DAYS = 12;

    public static LearningProposal Propose(PayeeId payeeId, IReadOnlyCollection<BillOccurrence> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count < MIN_OCCURRENCES)
            return LearningProposal.Refuse(payeeId, LearningRefusal.TooFewOccurrences, history.Count);

        var ordered = history.OrderBy(o => o.DueDate).ToList();

        if (CountAccounts(ordered) > MAX_ACCOUNTS_TO_LEARN)
            return LearningProposal.Refuse(payeeId, LearningRefusal.MultipleAccounts, history.Count);

        var gaps = new List<int>();
        for (var i = 1; i < ordered.Count; i++)
            gaps.Add(ordered[i].DueDate.DayNumber - ordered[i - 1].DueDate.DayNumber);

        var recurrence = Recurrence.ClosestTo(Median(gaps));
        if (recurrence is null)
            return LearningProposal.Refuse(payeeId, LearningRefusal.Irregular, history.Count);

        // Cada intervalo tem que caber, não só a mediana. Ocorrências em janeiro, fevereiro e julho
        // têm mediana de 90 dias e passariam por trimestrais — a mediana de dois intervalos
        // muito diferentes cai no meio deles, e o meio pode ser exatamente uma recorrência
        // conhecida. Sem esta conferência, compra avulsa viraria expectativa e alertaria por uma
        // conta que ninguém espera.
        if (gaps.Exists(g => Math.Abs(g - recurrence.IntervalDays) > IRREGULARITY_TOLERANCE_DAYS))
            return LearningProposal.Refuse(payeeId, LearningRefusal.Irregular, history.Count);

        // Um vencimento fora do padrão não pode redefinir o calendário inteiro, e a mediana é o
        // que resiste a ele — a média seria arrastada por um único mês atípico.
        var dueDay = Median(ordered.ConvertAll(o => o.DueDate.Day));
        var lead = Median(ordered.ConvertAll(o => Math.Max(0, o.DueDate.DayNumber - o.ArrivedOn.DayNumber)));

        // A ocorrência mais recente ancora a fase: é o vencimento observado mais próximo de hoje,
        // e portanto o que melhor descreve em que meses a conta cai daqui para a frente.
        var mostRecent = ordered[^1];
        var anchor = new CompetencePeriod(mostRecent.DueDate.Year, mostRecent.DueDate.Month);

        return LearningProposal.Propose(
            payeeId, recurrence, dueDay, lead, history.Count, anchor, MostFrequentSource(ordered));
    }

    /// <summary>
    /// Quantas contas distintas o histórico aparenta ter, pela sobreposição de competências.
    /// </summary>
    /// <remarks>
    /// Duas contas do mesmo beneficiário vencem no <strong>mesmo mês</strong>, então competência
    /// repetida é o sinal — e é um sinal deterministicamente observável, ao contrário de tentar
    /// ler a instalação do código de barras. O maior número de ocorrências num mesmo mês é o piso
    /// da contagem de contas.
    /// </remarks>
    private static int CountAccounts(IReadOnlyCollection<BillOccurrence> history)
        => history
            .GroupBy(o => (o.DueDate.Year, o.DueDate.Month))
            .Max(g => g.Count());

    /// <summary>
    /// A fonte por onde a conta mais chega, entre as ocorrências que têm fonte.
    /// </summary>
    /// <remarks>
    /// A moda, e não a mais recente: um mês em que a conta chegou por caminho atípico não pode
    /// redefinir para onde o alerta de captura vai olhar. Histórico só de importação manual não
    /// tem fonte, e aí não há hint a propor.
    /// </remarks>
    private static CaptureSourceId? MostFrequentSource(IReadOnlyCollection<BillOccurrence> history)
        => history
            .Where(o => o.SourceId is not null)
            .GroupBy(o => o.SourceId!.Value)
            .OrderByDescending(g => g.Count())
            .Select(g => (CaptureSourceId?)g.Key)
            .FirstOrDefault();

    private static int Median(List<int> values)
    {
        values.Sort();
        var middle = values.Count / 2;

        return values.Count % 2 == 1
            ? values[middle]
            : (int)Math.Round((values[middle - 1] + values[middle]) / 2d);
    }
}
