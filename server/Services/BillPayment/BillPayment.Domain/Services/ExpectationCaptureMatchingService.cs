namespace BillPayment.Domain.Services;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;

/// <summary>Qual ciclo de qual expectativa um artefato travado está bloqueando.</summary>
public sealed class ExpectationCaptureMatch : ValueObject
{
    public BillExpectationId ExpectationId { get; }
    public ExpectationCycleId CycleId { get; }

    private ExpectationCaptureMatch(BillExpectationId expectationId, ExpectationCycleId cycleId)
    {
        ExpectationId = expectationId;
        CycleId = cycleId;
    }

    internal static ExpectationCaptureMatch Of(BillExpectationId expectationId, ExpectationCycleId cycleId)
        => new(expectationId, cycleId);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExpectationId;
        yield return CycleId;
    }
}

/// <summary>
/// Cruza um artefato que chegou e travou com o ciclo de expectativa que ele estava vindo cumprir.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Domain Service porque cruza dois Aggregates</strong> — <c>CaptureItem</c> e
/// <c>BillExpectation</c> — e devolve valor: quem muta é <c>expectation.RecordCaptureFailure</c>.
/// Estático e puro, como os outros do BC.
/// </para>
/// <para>
/// <strong>Casa pela FONTE e pela janela do ciclo, porque não há mais nada com que casar.</strong>
/// Um item preso em <c>Locked</c>, <c>LinkFailed</c> ou <c>Failed</c> falhou antes da extração:
/// não tem beneficiário, não tem vencimento, não tem valor. O que sobra é por onde ele entrou —
/// e é exatamente para isso que <c>BillExpectation.HintSourceId</c> existe.
/// </para>
/// <para>
/// <strong>Ambiguidade devolve <c>null</c>, mesma doutrina do casamento de boleto.</strong> Duas
/// contas do mesmo tenant que chegam pela mesma caixa — quatro instalações da EDP, no arquivo
/// medido — não são desempatáveis por fonte. Marcar a errada apagaria o alerta da conta que de
/// fato não chegou e ainda mandaria a pessoa resolver um item que não é dela; deixar as duas em
/// <c>Waiting</c> só adia o aviso, que é o lado seguro de errar.
/// </para>
/// </remarks>
public static class ExpectationCaptureMatchingService
{
    /// <summary>
    /// Quantos dias depois do vencimento esperado um artefato ainda pode ser aquela conta.
    /// </summary>
    /// <remarks>
    /// A janela começa na abertura do ciclo — que já é o prazo de chegada aprendido — e se estende
    /// um pouco além do vencimento, porque a conta atrasada chega atrasada. Não vai além disso: um
    /// artefato que aparece semanas depois pertence ao ciclo seguinte, não a este.
    /// </remarks>
    public const int ARRIVAL_GRACE_DAYS = 3;

    /// <param name="candidates">
    /// As expectativas do tenant que apontam para esta fonte. Quem as carrega é o handler,
    /// filtrando por tenant e por <c>HintSourceId</c>.
    /// </param>
    /// <param name="arrivedOn">Quando o artefato entrou no sistema.</param>
    public static ExpectationCaptureMatch? Match(
        IReadOnlyCollection<BillExpectation> candidates,
        DateOnly arrivedOn,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var matches = candidates
            .Where(e => e.IsWatchingOn(today))
            .SelectMany(
                e => e.Cycles
                    .Where(c => c.Status.IsOpen && IsInWindow(e, c, arrivedOn))
                    .Select(c => ExpectationCaptureMatch.Of(e.Id, c.Id)))
            .Take(2)
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// O motivo do ciclo, traduzido do estado em que o artefato travou.
    /// </summary>
    /// <remarks>
    /// A tradução mora no domínio, e não no handler, porque é ela que decide qual dos dois avisos
    /// a pessoa recebe — <c>MissReason.Arrived</c> separa "vá buscar" de "resolva este item".
    /// Estado que não aguarda resgate devolve <c>null</c>: não há falha de captura a registrar.
    /// </remarks>
    public static MissReason? ReasonFor(CaptureItemStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status == CaptureItemStatus.Locked)
            return MissReason.Locked;
        if (status == CaptureItemStatus.LinkFailed)
            return MissReason.LinkFailed;
        if (status == CaptureItemStatus.Unrouted)
            return MissReason.Unrouted;

        // `Unrecognized` e `Failed` colapsam em CaptureFailed de propósito: para quem espera a
        // conta, "não achei boleto aqui" e "a leitura estourou" pedem a mesma ação — abrir o item
        // e digitar a linha. A distinção continua visível no próprio CaptureItem.
        return status == CaptureItemStatus.Unrecognized || status == CaptureItemStatus.Failed
            ? MissReason.CaptureFailed
            : null;
    }

    private static bool IsInWindow(BillExpectation expectation, ExpectationCycle cycle, DateOnly arrivedOn)
        => arrivedOn >= expectation.OpensAtFor(cycle.Competence)
            && arrivedOn <= cycle.ExpectedDueDate.AddDays(ARRIVAL_GRACE_DAYS);
}
