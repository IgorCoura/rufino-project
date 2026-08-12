namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O que o tenant espera receber, e o aviso de quando não recebeu.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não é conveniência: é requisito de arquitetura (ADR-014).</strong> Sem DDA, nenhum
/// canal garante que a conta foi emitida — nem e-mail, nem portal. Automatizar a captura sem
/// isto <em>aumenta</em> o risco de esquecimento, porque troca a conferência manual (que ao menos
/// falha de forma visível) por uma automação que falha em silêncio. A primeira notícia da falha
/// passaria a ser a multa.
/// </para>
/// <para>
/// <strong>A chave inclui a referência de conta, e ela é informada — não deduzida.</strong>
/// Medido em 2026-08-12 sobre o arquivo real: <strong>10 dos 20 grupos de beneficiário têm mais
/// de uma conta do mesmo tenant</strong> (quatro instalações da EDP, três do DAE). Uma
/// expectativa por beneficiário seria cumprida pela primeira conta que chegasse e esconderia as
/// outras — que é exatamente a falha silenciosa que este agregado existe para impedir. A
/// referência está no campo livre do código de barras em arrecadação, mas em posição que muda
/// por emissor, então deduzi-la seria adivinhação: quem a informa é quem cadastra, e o
/// aprendizado automático <strong>recusa</strong> aprender quando o histórico mostra mais de uma
/// conta (ver <c>ExpectationLearningService</c>).
/// </para>
/// </remarks>
public sealed class BillExpectation : AggregateRoot<BillExpectationId>
{
    public const int LABEL_MAX_LENGTH = 200;
    public const int ACCOUNT_REFERENCE_MAX_LENGTH = 100;
    public const int DEACTIVATION_REASON_MAX_LENGTH = 200;

    /// <summary>Piso da antecedência do alerta — avisar no próprio dia não dá tempo de agir.</summary>
    public const int MIN_ALERT_LEAD_DAYS = 1;

    /// <summary>Folga sobre o prazo observado, para o alerta não disparar no dia em que a conta costuma chegar.</summary>
    public const int ALERT_LEAD_SLACK_DAYS = 2;

    /// <summary>Piso do default, para conta que chega em cima da hora ainda dar tempo de reagir.</summary>
    public const int DEFAULT_MIN_ALERT_LEAD_DAYS = 3;

    /// <summary>
    /// Ciclos <c>Missing</c> consecutivos e não reivindicados que desativam a expectativa.
    /// </summary>
    /// <remarks>
    /// Silêncio do usuário diante de três alertas seguidos é sinal de que a expectativa morreu —
    /// o imóvel foi vendido, o contrato encerrou. Continuar alertando treinaria a pessoa a
    /// ignorar alerta, e alerta ignorado destrói o mecanismo inteiro.
    /// </remarks>
    public const int CONSECUTIVE_MISSES_TO_DEACTIVATE = 3;

    private readonly List<ExpectationCycle> _cycles = [];

    public TenantId TenantId { get; private set; }
    public PayeeId PayeeId { get; private set; }

    /// <summary>
    /// O identificador da conta junto ao beneficiário — instalação, matrícula, conta contrato.
    /// Vazio quando o tenant tem uma conta só com aquele beneficiário.
    /// </summary>
    public string AccountReference { get; private set; } = string.Empty;

    /// <summary>O que a pessoa lê no alerta: "EDP — Casa Florentino".</summary>
    public string Label { get; private set; } = string.Empty;

    public Recurrence Recurrence { get; private set; } = default!;

    /// <summary>Dia do mês em que a conta costuma vencer.</summary>
    public int ExpectedDueDay { get; private set; }

    /// <summary>Dias entre a chegada e o vencimento, observados. Alimenta o default do alerta.</summary>
    public int ObservedLeadDays { get; private set; }

    /// <summary>Antecedência com que a ausência vira alerta.</summary>
    public int AlertLeadDays { get; private set; }

    public ExpectationOrigin Origin { get; private set; } = default!;

    /// <summary>Quantas ocorrências alimentaram o aprendizado. Cresce a cada cumprimento.</summary>
    public int ObservationCount { get; private set; }

    /// <summary>Por onde a conta costuma chegar — vira o link acionável do alerta.</summary>
    public CaptureSourceId? HintSourceId { get; private set; }

    public bool IsActive { get; private set; }
    public DateOnly? PausedUntil { get; private set; }
    public string? DeactivationReason { get; private set; }

    public IReadOnlyCollection<ExpectationCycle> Cycles => _cycles.AsReadOnly();

    private BillExpectation() { }

    private BillExpectation(BillExpectationId id) : base(id) { }

    /// <summary>Cadastro manual — cobre a conta que o histórico ainda não alcança.</summary>
    public static BillExpectation Register(
        TenantId tenantId,
        PayeeId payeeId,
        string? accountReference,
        string label,
        Recurrence recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int? alertLeadDays,
        DateTime occurredAt)
        => Create(
            tenantId, payeeId, accountReference, label, recurrence, expectedDueDay,
            observedLeadDays, alertLeadDays, ExpectationOrigin.Manual, observationCount: 0,
            hintSourceId: null, occurredAt);

    /// <summary>
    /// Nasce do histórico. <paramref name="observationCount"/> é quantas ocorrências a
    /// sustentaram — quem apura é o <c>ExpectationLearningService</c>, que também é quem recusa
    /// aprender quando não há regularidade ou quando há mais de uma conta do mesmo beneficiário.
    /// </summary>
    public static BillExpectation Learn(
        TenantId tenantId,
        PayeeId payeeId,
        string label,
        Recurrence recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int observationCount,
        CaptureSourceId? hintSourceId,
        DateTime occurredAt)
    {
        var expectation = Create(
            tenantId, payeeId, accountReference: null, label, recurrence, expectedDueDay,
            observedLeadDays, alertLeadDays: null, ExpectationOrigin.Learned, observationCount,
            hintSourceId, occurredAt);

        // Criar em silêncio seria pior que não criar: a primeira notícia da existência da
        // expectativa seria um alerta que o usuário não pediu.
        expectation.AddDomainEvent(new BillExpectationLearnedDomainEvent(
            expectation.Id, tenantId, expectation.Label, recurrence.Name, occurredAt));

        return expectation;
    }

    private static BillExpectation Create(
        TenantId tenantId,
        PayeeId payeeId,
        string? accountReference,
        string label,
        Recurrence recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int? alertLeadDays,
        ExpectationOrigin origin,
        int observationCount,
        CaptureSourceId? hintSourceId,
        DateTime occurredAt)
    {
        if (payeeId.Equals(PayeeId.Empty))
            throw BillExpectationErrors.PayeeRequired();
        if (recurrence is null)
            throw BillExpectationErrors.RecurrenceRequired();

        var expectation = new BillExpectation(BillExpectationId.New())
        {
            TenantId = tenantId,
            PayeeId = payeeId,
            Recurrence = recurrence,
            Origin = origin,
            ObservationCount = observationCount < 0 ? 0 : observationCount,
            HintSourceId = hintSourceId,
            IsActive = true,
        };

        expectation.SetAccountReference(accountReference);
        expectation.SetLabel(label);
        expectation.SetExpectedDueDay(expectedDueDay);
        expectation.ObservedLeadDays = observedLeadDays < 0 ? 0 : observedLeadDays;
        expectation.SetAlertLeadDays(alertLeadDays ?? DefaultAlertLead(expectation.ObservedLeadDays));

        expectation.CreatedAt = occurredAt;
        expectation.UpdatedAt = occurredAt;
        return expectation;
    }

    /// <summary>
    /// Abre o ciclo de uma competência. Chamado pelo job periódico.
    /// </summary>
    /// <remarks>
    /// A data de alerta sai de <c>ExpectedDueDate - AlertLeadDays</c>, e o lead é aprendido. Uma
    /// regra fixa de "avise três dias antes" alertaria cedo demais para a conta que chega em cima
    /// da hora e tarde demais para a que chega com folga — e o alerta cedo demais é o que treina
    /// o usuário a ignorá-lo.
    /// </remarks>
    public ExpectationCycle OpenCycle(CompetencePeriod competence, DateTime occurredAt)
    {
        EnsureActive();

        if (_cycles.Exists(c => c.Competence.Equals(competence)))
            throw BillExpectationErrors.CycleAlreadyOpen(competence.ToString());

        var dueDate = DueDateIn(competence);
        var cycle = new ExpectationCycle(competence, dueDate, dueDate.AddDays(-AlertLeadDays), occurredAt);

        _cycles.Add(cycle);
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationCycleOpenedDomainEvent(
            Id, TenantId, cycle.Id, competence.ToString(), dueDate, occurredAt));

        return cycle;
    }

    /// <summary>
    /// A conta chegou e virou boleto. <strong>Aprende</strong>: reajusta o dia de vencimento e o
    /// prazo observado por média móvel sobre as ocorrências já vistas.
    /// </summary>
    /// <remarks>
    /// Média móvel, e não substituição, porque um mês atípico não pode redefinir a janela inteira
    /// — e não pode, tampouco, ser ignorado: concessionária muda calendário de faturamento.
    /// </remarks>
    public void Fulfill(ExpectationCycleId cycleId, BillId billId, DateOnly actualDueDate, DateTime occurredAt)
    {
        var cycle = RequireCycle(cycleId);

        cycle.Fulfill(billId, occurredAt);

        var arrivedAt = DateOnly.FromDateTime(occurredAt);
        var lead = actualDueDate.DayNumber - arrivedAt.DayNumber;

        ObservationCount++;
        ExpectedDueDay = MovingAverage(ExpectedDueDay, actualDueDate.Day);
        ObservedLeadDays = MovingAverage(ObservedLeadDays, lead < 0 ? 0 : lead);

        // O default acompanha o que foi aprendido; antecedência escolhida à mão não é sobrescrita.
        if (Origin == ExpectationOrigin.Learned)
            SetAlertLeadDays(DefaultAlertLead(ObservedLeadDays));

        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationFulfilledDomainEvent(
            Id, TenantId, cycle.Id, billId, occurredAt));
    }

    /// <summary>
    /// Chegou algo e não deu para transformar em boleto — cumprimento parcial.
    /// </summary>
    /// <remarks>
    /// É o alerta mais valioso dos dois: o sistema <em>já tem</em> o documento e sabe o que
    /// falta, então o aviso leva direto ao item resolvível — informar a senha, reivindicar,
    /// digitar a linha. Não conta como observação: nada foi aprendido sobre o calendário.
    /// </remarks>
    public void RecordCaptureFailure(
        ExpectationCycleId cycleId,
        CaptureItemId itemId,
        MissReason reason,
        DateTime occurredAt)
    {
        var cycle = RequireCycle(cycleId);

        cycle.RecordCaptureFailure(itemId, reason, occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationCaptureFailedDomainEvent(
            Id, TenantId, cycle.Id, itemId, reason.Name, occurredAt));
    }

    /// <summary>Passou da data de alerta sem cumprimento.</summary>
    public void MarkMissing(ExpectationCycleId cycleId, MissReason reason, DateOnly today, DateTime occurredAt)
    {
        var cycle = RequireCycle(cycleId);

        cycle.MarkMissing(reason, today, occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationMissedDomainEvent(
            Id, TenantId, cycle.Id, reason.Name, occurredAt));
    }

    /// <summary>
    /// Registra que o alerta de um nível saiu, e devolve <c>false</c> quando ele já havia saído.
    /// </summary>
    public bool TryRecordAlert(ExpectationCycleId cycleId, AlertLevel level, DateTime occurredAt)
    {
        var recorded = RequireCycle(cycleId).TryRecordAlert(level, occurredAt);

        if (recorded)
            UpdatedAt = occurredAt;

        return recorded;
    }

    /// <summary>"Este mês não vem" — sem desativar a expectativa.</summary>
    public void Waive(ExpectationCycleId cycleId, UserId waivedBy, string? reason, DateTime occurredAt)
    {
        RequireCycle(cycleId).Waive(waivedBy, reason, occurredAt);
        UpdatedAt = occurredAt;
    }

    /// <summary>Imóvel desocupado, obra parada, férias.</summary>
    public void Pause(DateOnly until, DateTime occurredAt)
    {
        PausedUntil = until;
        UpdatedAt = occurredAt;
    }

    public void Resume(DateTime occurredAt)
    {
        PausedUntil = null;
        UpdatedAt = occurredAt;
    }

    public void Deactivate(string? reason, DateTime occurredAt)
    {
        var trimmed = reason?.Trim();
        if (trimmed is { Length: > DEACTIVATION_REASON_MAX_LENGTH })
            throw BillExpectationErrors.TextTooLong(nameof(DeactivationReason), DEACTIVATION_REASON_MAX_LENGTH);

        IsActive = false;
        DeactivationReason = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        UpdatedAt = occurredAt;
    }

    public void Reactivate(DateTime occurredAt)
    {
        IsActive = true;
        DeactivationReason = null;
        PausedUntil = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>Está valendo hoje — ativa e fora da pausa.</summary>
    public bool IsWatchingOn(DateOnly today)
        => IsActive && (PausedUntil is null || today > PausedUntil.Value);

    /// <summary>
    /// A expectativa morreu de silêncio: <c>CONSECUTIVE_MISSES_TO_DEACTIVATE</c> ciclos seguidos
    /// não cumpridos e nunca dispensados.
    /// </summary>
    /// <remarks>
    /// Conta os mais recentes em ordem e para no primeiro que não seja falha — um cumprimento no
    /// meio zera a sequência, porque prova que a conta continua existindo.
    /// </remarks>
    public bool ShouldDeactivateForSilence()
    {
        var streak = 0;

        var mostRecentFirst = _cycles
            .OrderByDescending(c => c.Competence.Year)
            .ThenByDescending(c => c.Competence.Month)
            .Select(c => c.Status);

        foreach (var status in mostRecentFirst)
        {
            if (status == CycleStatus.Missing)
            {
                streak++;
                if (streak >= CONSECUTIVE_MISSES_TO_DEACTIVATE)
                    return true;

                continue;
            }

            // Ciclo ainda aguardando não conta nem quebra: ele simplesmente não se pronunciou.
            if (status == CycleStatus.Waiting)
                continue;

            return false;
        }

        return false;
    }

    /// <summary>O ciclo aberto de uma competência, se houver.</summary>
    public ExpectationCycle? CycleFor(CompetencePeriod competence)
        => _cycles.Find(c => c.Competence.Equals(competence));

    /// <summary>
    /// O vencimento esperado numa competência, com o dia aprendido ajustado ao tamanho do mês.
    /// </summary>
    /// <remarks>
    /// Dia 31 numa competência de trinta dias não existe: sem o ajuste, a construção da data
    /// lançaria e o job pararia de abrir ciclos justamente para as contas que vencem no fim do mês.
    /// </remarks>
    public DateOnly DueDateIn(CompetencePeriod competence)
    {
        var daysInMonth = DateTime.DaysInMonth(competence.Year, competence.Month);

        return new DateOnly(competence.Year, competence.Month, Math.Min(ExpectedDueDay, daysInMonth));
    }

    private ExpectationCycle RequireCycle(ExpectationCycleId cycleId)
        => _cycles.Find(c => c.Id == cycleId)
            ?? throw BillExpectationErrors.CycleNotFound(cycleId.Value);

    private void EnsureActive()
    {
        if (!IsActive)
            throw BillExpectationErrors.Inactive();
    }

    private static int DefaultAlertLead(int observedLeadDays)
        => Math.Max(DEFAULT_MIN_ALERT_LEAD_DAYS, observedLeadDays + ALERT_LEAD_SLACK_DAYS);

    /// <summary>
    /// Média móvel simples sobre as observações já feitas. Com uma observação só, o valor novo
    /// substitui — não há média a fazer.
    /// </summary>
    private int MovingAverage(int current, int observed)
        => ObservationCount <= 1 ? observed : (int)Math.Round((current * (ObservationCount - 1d) + observed) / ObservationCount);

    private void SetAccountReference(string? value)
    {
        var trimmed = value?.Trim();
        if (trimmed is { Length: > ACCOUNT_REFERENCE_MAX_LENGTH })
            throw BillExpectationErrors.TextTooLong(nameof(AccountReference), ACCOUNT_REFERENCE_MAX_LENGTH);

        // String vazia, e não nulo: ela entra num índice único, e no Postgres NULL não colide
        // com NULL — duas expectativas sem referência passariam pelo banco.
        AccountReference = string.IsNullOrEmpty(trimmed) ? string.Empty : trimmed;
    }

    private void SetLabel(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw BillExpectationErrors.LabelRequired();
        if (trimmed.Length > LABEL_MAX_LENGTH)
            throw BillExpectationErrors.TextTooLong(nameof(Label), LABEL_MAX_LENGTH);

        Label = trimmed;
    }

    private void SetExpectedDueDay(int value)
    {
        if (value is < 1 or > 31)
            throw BillExpectationErrors.InvalidDueDay();

        ExpectedDueDay = value;
    }

    private void SetAlertLeadDays(int value)
    {
        var maximum = Recurrence.IntervalDays - 1;

        if (value < MIN_ALERT_LEAD_DAYS || value > maximum)
            throw BillExpectationErrors.InvalidAlertLead(maximum);

        AlertLeadDays = value;
    }
}
