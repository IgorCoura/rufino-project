namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Uma ocorrência esperada — a conta de uma competência — e o que aconteceu com ela.
/// </summary>
/// <remarks>
/// <para>
/// Entidade interna: só a <see cref="BillExpectation"/> cria e muta, e ela nunca emite Domain
/// Event — quem emite é a raiz.
/// </para>
/// <para>
/// <strong>Um ciclo não fecha quando falha.</strong> <c>Missing</c> e <c>PartiallyCaptured</c>
/// continuam abertos porque a conta ainda pode ser destravada, reivindicada ou digitada à mão
/// dias depois; fechar no primeiro tropeço perderia o cumprimento tardio e deixaria o painel
/// mentindo sobre o que ainda é resolvível.
/// </para>
/// </remarks>
public sealed class ExpectationCycle : Entity<ExpectationCycleId>
{
    public const int WAIVE_REASON_MAX_LENGTH = 200;

    private readonly List<AlertRecord> _alerts = [];

    public CompetencePeriod Competence { get; private set; } = default!;

    /// <summary>Quando a conta desta competência deve vencer.</summary>
    public DateOnly ExpectedDueDate { get; private set; }

    /// <summary>
    /// A partir de quando a ausência vira alerta. É <strong>aprendido</strong>, não fixo — conta
    /// que costuma chegar com folga e conta que chega em cima da hora não podem avisar no mesmo dia.
    /// </summary>
    public DateOnly AlertAt { get; private set; }

    public CycleStatus Status { get; private set; } = default!;

    public BillId? FulfilledByBillId { get; private set; }

    /// <summary>O artefato que chegou e não deu para ler. É o link acionável do alerta.</summary>
    public CaptureItemId? BlockedByCaptureItemId { get; private set; }

    public MissReason? MissReason { get; private set; }

    public UserId? WaivedBy { get; private set; }
    public string? WaiveReason { get; private set; }

    public IReadOnlyCollection<AlertRecord> Alerts => _alerts.AsReadOnly();

    private ExpectationCycle() { }

    internal ExpectationCycle(
        CompetencePeriod competence,
        DateOnly expectedDueDate,
        DateOnly alertAt,
        DateTime occurredAt) : base(ExpectationCycleId.New())
    {
        Competence = competence;
        ExpectedDueDate = expectedDueDate;
        AlertAt = alertAt;
        Status = CycleStatus.Waiting;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    internal void Fulfill(BillId billId, DateTime occurredAt)
    {
        EnsureOpen();

        Status = CycleStatus.Fulfilled;
        FulfilledByBillId = billId;
        MissReason = null;

        // O ponteiro para o artefato travado morre com o cumprimento: ele era o link de "resolva
        // este item", e o item deixou de ser o que falta.
        BlockedByCaptureItemId = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Registra o artefato que chegou e não deu para ler, e diz se algo mudou.
    /// </summary>
    /// <remarks>
    /// <strong>Devolver <c>bool</c> é o que sustenta a idempotência do outbox.</strong> A mesma
    /// falha do mesmo item reentregue não pode emitir o aviso de novo — e conferir isso aqui, em
    /// vez de no handler, mantém a regra num lugar só.
    /// </remarks>
    internal bool RecordCaptureFailure(CaptureItemId itemId, MissReason reason, DateTime occurredAt)
    {
        EnsureOpen();

        if (Status == CycleStatus.PartiallyCaptured
            && BlockedByCaptureItemId == itemId
            && MissReason == reason)
        {
            return false;
        }

        Status = CycleStatus.PartiallyCaptured;
        BlockedByCaptureItemId = itemId;
        MissReason = reason;
        UpdatedAt = occurredAt;
        return true;
    }

    /// <summary>
    /// O artefato que travava este ciclo foi resolvido sem virar boleto — volta a esperar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Volta para <c>Waiting</c>, e não para <c>Missing</c>: a conta ainda pode chegar por outro
    /// caminho, e quem decide que ela não chegou é a varredura, ao passar da data de alerta.
    /// </para>
    /// <para>
    /// <strong>Os alertas já enviados ficam.</strong> Eles são história do que a pessoa recebeu;
    /// apagá-los faria o mesmo nível sair de novo no dia seguinte, que é exatamente a repetição
    /// que o registro por nível existe para impedir.
    /// </para>
    /// </remarks>
    internal bool ClearCaptureFailure(CaptureItemId itemId, DateTime occurredAt)
    {
        if (Status != CycleStatus.PartiallyCaptured || BlockedByCaptureItemId != itemId)
            return false;

        Status = CycleStatus.Waiting;
        BlockedByCaptureItemId = null;
        MissReason = null;
        UpdatedAt = occurredAt;
        return true;
    }

    internal void MarkMissing(MissReason reason, DateOnly today, DateTime occurredAt)
    {
        EnsureOpen();

        if (today < AlertAt)
            throw BillExpectationErrors.TooEarlyToMiss();

        Status = CycleStatus.Missing;
        MissReason = reason;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Reposiciona o ciclo depois de a expectativa ter o calendário corrigido.
    /// </summary>
    /// <remarks>
    /// <strong>Só vale para quem ainda espera.</strong> Ciclo que já se pronunciou — cumprido,
    /// dispensado, não cumprido, parcialmente capturado — é história, e mexer na data dele
    /// reescreveria o passado: um <c>Missing</c> redatado para o futuro voltaria a alertar por
    /// uma conta que o usuário já resolveu. Sair calado, e não lançar, porque quem chama é a raiz
    /// varrendo a coleção inteira: a seleção é dela, esta guarda é a segunda tranca.
    /// </remarks>
    internal void Reschedule(DateOnly expectedDueDate, DateOnly alertAt, DateTime occurredAt)
    {
        if (Status != CycleStatus.Waiting)
            return;

        if (ExpectedDueDate == expectedDueDate && AlertAt == alertAt)
            return;

        ExpectedDueDate = expectedDueDate;
        AlertAt = alertAt;
        UpdatedAt = occurredAt;
    }

    internal void Waive(UserId waivedBy, string? reason, DateTime occurredAt)
    {
        EnsureOpen();

        var trimmed = reason?.Trim();
        if (trimmed is { Length: > WAIVE_REASON_MAX_LENGTH })
            throw BillExpectationErrors.TextTooLong(nameof(WaiveReason), WAIVE_REASON_MAX_LENGTH);

        Status = CycleStatus.Waived;
        WaivedBy = waivedBy;
        WaiveReason = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Registra o alerta e diz se ele é novo. <strong>Devolver <c>bool</c> em vez de deixar o
    /// chamador conferir</strong> é o que garante a regra de não repetir nível num lugar só.
    /// </summary>
    internal bool TryRecordAlert(AlertLevel level, DateTime occurredAt)
    {
        if (_alerts.Exists(a => a.Level == level))
            return false;

        _alerts.Add(AlertRecord.Of(level, occurredAt));
        UpdatedAt = occurredAt;
        return true;
    }

    private void EnsureOpen()
    {
        if (!Status.IsOpen)
            throw BillExpectationErrors.CycleNotOpen(Status.Name);
    }
}
