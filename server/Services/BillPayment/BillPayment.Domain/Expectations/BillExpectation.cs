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
/// <strong>Esperar e avisar são dois prazos, não um.</strong> <see cref="ObservedLeadDays"/> diz
/// quando a conta <em>chega</em> e governa a abertura do ciclo; <see cref="AlertLeadDays"/> diz
/// quando a ausência dela vira <em>aviso</em>. Até 2026-08-27 os dois papéis moravam no segundo
/// campo, e o efeito era o oposto do propósito do agregado: uma conta que chega vinte dias antes
/// do vencimento, com aviso pedido para dois dias antes, encontrava o ciclo fechado ao chegar,
/// não cumpria nada, e disparava "não chegou" sobre uma conta capturada e aprovada.
/// </para>
/// <para>
/// <strong>A chave inclui a referência de conta, e ela é informada — não deduzida.</strong>
/// Medido em 2026-08-12 sobre o arquivo real: <strong>10 dos 20 grupos de beneficiário têm mais
/// de uma conta do mesmo tenant</strong> (quatro instalações da EDP, três do DAE). Uma
/// expectativa por beneficiário seria cumprida pela primeira conta que chegasse e esconderia as
/// outras — que é exatamente a falha silenciosa que este agregado existe para impedir.
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
    /// Folga sobre o prazo observado para o ciclo <strong>abrir antes</strong> da chegada habitual.
    /// </summary>
    /// <remarks>
    /// Abrir cedo não custa nada — o ciclo nasce <c>Waiting</c>, não alerta e não entra no painel
    /// de "vence em breve" —, e cobre o mês em que a conta chega adiantada. Abrir tarde custa o
    /// mecanismo inteiro: o boleto chega, não encontra ciclo, e a ausência dele vira alerta.
    /// </remarks>
    public const int OPEN_LEAD_SLACK_DAYS = 5;

    /// <summary>
    /// Teto do prazo observado. <strong>Desamarrado do intervalo da recorrência</strong> de
    /// propósito: conta mensal que chega dois meses antes é fato do arquivo real, e o teto antigo
    /// (o próprio intervalo) proibia exatamente a configuração que o agregado precisa suportar.
    /// Dois ciclos abertos ao mesmo tempo deixaram de ser problema quando o casamento passou a
    /// resolver por competência — ver <c>ExpectationMatchingService</c>.
    /// </summary>
    public const int MAX_OBSERVED_LEAD_DAYS = 180;

    /// <summary>
    /// Quantas competências à frente a varredura olha antes de desistir.
    /// </summary>
    /// <remarks>
    /// Catorze cobre de mensal a anual com folga. O laço quase sempre para muito antes, na
    /// primeira competência cuja data de abertura ainda não chegou — elas crescem em ordem.
    /// </remarks>
    public const int MAX_COMPETENCES_AHEAD = 14;

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

    /// <summary>
    /// Uma competência em que a conta <strong>vence</strong>. Define a fase da recorrência.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ExpectedDueDay"/> diz o dia; sozinho ele não diz em quais <em>meses</em> uma
    /// conta trimestral vence. Sem esta âncora a varredura abria um ciclo por mês para toda
    /// expectativa — inclusive as anuais, que assim geravam doze ciclos por ano, onze deles
    /// <c>Missing</c>, e se autodesativavam em três meses pela regra do silêncio.
    /// </para>
    /// <para>
    /// <strong>Ela se corrige sozinha:</strong> todo cumprimento reancora a fase na competência
    /// que de fato chegou. Um calendário de faturamento que anda de mês é absorvido no primeiro
    /// boleto seguinte, sem ninguém reconfigurar nada.
    /// </para>
    /// </remarks>
    public CompetencePeriod AnchorCompetence { get; private set; } = default!;

    /// <summary>
    /// Dias entre a chegada e o vencimento, observados. <strong>É o prazo que abre o ciclo.</strong>
    /// </summary>
    public int ObservedLeadDays { get; private set; }

    /// <summary>Antecedência com que a <em>ausência</em> vira alerta.</summary>
    public int AlertLeadDays { get; private set; }

    public ExpectationOrigin Origin { get; private set; } = default!;

    /// <summary>Quantas ocorrências alimentaram o aprendizado. Cresce a cada cumprimento.</summary>
    public int ObservationCount { get; private set; }

    /// <summary>
    /// Por onde a conta costuma chegar — vira o link acionável do alerta.
    /// </summary>
    /// <remarks>
    /// <strong>É a única ponte entre um artefato que falhou e a conta que ele seria.</strong> Um
    /// <c>CaptureItem</c> preso em <c>Locked</c> ou <c>LinkFailed</c> falhou <em>antes</em> da
    /// extração: não tem beneficiário nem vencimento, e portanto não há nada nele para casar com
    /// uma expectativa a não ser a fonte por onde entrou. Enquanto este campo ficou nulo — e ele
    /// ficou nulo em todo o código de produção até 2026-08-27 — o alerta "chegou e não consegui
    /// ler" era inalcançável.
    /// </remarks>
    public CaptureSourceId? HintSourceId { get; private set; }

    public bool IsActive { get; private set; }
    public DateOnly? PausedUntil { get; private set; }
    public string? DeactivationReason { get; private set; }

    /// <summary>
    /// Desde quando esta expectativa está de fato vigiando.
    /// </summary>
    /// <remarks>
    /// <strong>Piso contra o falso positivo de boas-vindas.</strong> Cadastrar
    /// uma expectativa no dia 25, ou retomá-la depois de três meses de pausa, encontraria
    /// competências cuja data de alerta já passou — e a varredura as abriria só para marcá-las
    /// como não cumpridas no mesmo instante, alertando por contas que ninguém pediu para vigiar.
    /// </remarks>
    public DateTime WatchingSince { get; private set; }

    /// <summary>
    /// Quando a varredura passou por aqui pela última vez. <strong>Não é <c>UpdatedAt</c></strong>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A fila do job ordenava por <c>UpdatedAt</c>, que só muda quando há mudança de negócio. O
    /// efeito era uma <em>inversão de prioridade</em>: expectativa parada mantinha o carimbo
    /// antigo e ocupava as vagas do lote para sempre, enquanto a que estava sendo cumprida e
    /// alertada ganhava carimbo novo e ia para o fim da fila. Passando de cem expectativas ativas
    /// na instalação, as demais nunca eram varridas — sem erro nenhum no log.
    /// </para>
    /// <para>
    /// Este carimbo é gravado em <strong>toda</strong> passagem, inclusive na que não faz nada, e
    /// é ele que faz a fila girar. <c>UpdatedAt</c> continua significando mudança de negócio.
    /// </para>
    /// </remarks>
    public DateTime LastSweptAt { get; private set; }

    public IReadOnlyCollection<ExpectationCycle> Cycles => _cycles.AsReadOnly();

    private BillExpectation() { }

    private BillExpectation(BillExpectationId id) : base(id) { }

    /// <summary>
    /// Quantos dias antes do vencimento o ciclo passa a <strong>esperar</strong> a conta.
    /// </summary>
    /// <remarks>
    /// O <c>Max</c> com a antecedência do alerta fecha o caso patológico de alguém pedir aviso com
    /// mais antecedência do que a conta chega: sem ele, o ciclo nasceria depois da própria data de
    /// alerta e seria marcado como não cumprido no mesmo instante em que foi aberto.
    /// </remarks>
    public int OpenLeadDays => Math.Max(ObservedLeadDays + OPEN_LEAD_SLACK_DAYS, AlertLeadDays);

    /// <summary>Quando o ciclo de uma competência passa a esperar a conta.</summary>
    public DateOnly OpensAtFor(CompetencePeriod competence) => DueDateIn(competence).AddDays(-OpenLeadDays);

    /// <summary>Quando a ausência da conta daquela competência vira alerta.</summary>
    public DateOnly AlertAtFor(CompetencePeriod competence) => DueDateIn(competence).AddDays(-AlertLeadDays);

    /// <summary>Se a competência cai na cadência desta expectativa.</summary>
    public bool IsOnSchedule(CompetencePeriod competence)
    {
        ArgumentNullException.ThrowIfNull(competence);

        var distance = MonthNumber(competence) - MonthNumber(AnchorCompetence);
        var interval = Recurrence.IntervalMonths;

        // Resto de negativo é negativo em C#: sem o segundo módulo, toda competência anterior à
        // âncora ficaria fora da cadência — e o ciclo do mês corrente nunca abriria numa
        // expectativa que o cadastro manual ancorou no futuro.
        return (((distance % interval) + interval) % interval) == 0;
    }

    /// <summary>
    /// Cadastro manual — cobre a conta que o histórico ainda não alcança.
    /// </summary>
    /// <param name="anchorDueDate">
    /// Um vencimento conhecido da conta, que fixa a fase da recorrência. Só importa fora do
    /// mensal — bimestral, trimestral e anual precisam saber em quais meses a conta cai. Ausente,
    /// a fase é o mês do cadastro.
    /// </param>
    public static BillExpectation Register(
        TenantId tenantId,
        PayeeId payeeId,
        string? accountReference,
        string label,
        Recurrence recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int? alertLeadDays,
        DateOnly? anchorDueDate,
        CaptureSourceId? hintSourceId,
        DateTime occurredAt)
        => Create(
            tenantId, payeeId, accountReference, label, recurrence, expectedDueDay,
            observedLeadDays, alertLeadDays, ExpectationOrigin.Manual, observationCount: 0,
            CompetenceOf(anchorDueDate), hintSourceId, occurredAt);

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
        CompetencePeriod? anchorCompetence,
        CaptureSourceId? hintSourceId,
        DateTime occurredAt)
    {
        var expectation = Create(
            tenantId, payeeId, accountReference: null, label, recurrence, expectedDueDay,
            observedLeadDays, alertLeadDays: null, ExpectationOrigin.Learned, observationCount,
            anchorCompetence, hintSourceId, occurredAt);

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
        CompetencePeriod? anchorCompetence,
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

            // Mensal ignora a âncora (todo mês cai na cadência); nas demais, ausente significa
            // "a conta vence neste mês", que é o que quem cadastra sem informar nada quer dizer.
            AnchorCompetence = anchorCompetence
                ?? new CompetencePeriod(occurredAt.Year, occurredAt.Month),
        };

        expectation.SetAccountReference(accountReference);
        expectation.SetLabel(label);
        expectation.SetExpectedDueDay(expectedDueDay);
        expectation.SetObservedLeadDays(observedLeadDays);
        expectation.SetAlertLeadDays(alertLeadDays ?? expectation.DefaultAlertLead());

        expectation.CreatedAt = occurredAt;
        expectation.UpdatedAt = occurredAt;
        expectation.WatchingSince = occurredAt;
        expectation.LastSweptAt = occurredAt;
        return expectation;
    }

    /// <summary>
    /// Corrige o cadastro da expectativa. <strong>O beneficiário não entra</strong> — trocá-lo é
    /// excluir e cadastrar de novo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Reconfigurar torna a expectativa <c>Manual</c>, mesmo que ela tenha nascido do
    /// histórico.</strong> <see cref="Fulfill"/> reajusta a antecedência sozinho enquanto a
    /// origem for <c>Learned</c>, então uma edição que não virasse a origem seria desfeita no
    /// próximo cumprimento — em silêncio, que é a classe de falha que este agregado existe para
    /// impedir. O aprendizado do calendário continua (o dia de vencimento e o prazo observado
    /// seguem em média móvel); o que passa a ser respeitado é a antecedência escolhida à mão.
    /// </para>
    /// <para>
    /// <strong>A expectativa é a configuração; o ciclo é a história.</strong> Só os ciclos que
    /// ainda esperam são redatados — ver <c>ExpectationCycle.Reschedule</c>. E os que ainda
    /// esperam e deixaram de pertencer à cadência são <em>removidos</em>: eles descrevem um mês
    /// que a expectativa reconfigurada não espera mais, e alertariam por ele.
    /// </para>
    /// </remarks>
    public void Reconfigure(
        string? accountReference,
        string label,
        Recurrence recurrence,
        int expectedDueDay,
        int observedLeadDays,
        int? alertLeadDays,
        DateOnly? anchorDueDate,
        DateTime occurredAt)
    {
        if (recurrence is null)
            throw BillExpectationErrors.RecurrenceRequired();

        SetAccountReference(accountReference);
        SetLabel(label);
        SetExpectedDueDay(expectedDueDay);
        SetObservedLeadDays(observedLeadDays);

        // A recorrência ANTES da antecedência: é ela que define o teto, e na ordem inversa a
        // antecedência seria conferida contra o intervalo antigo — recusando valor válido, ou
        // aceitando valor que o novo intervalo não comporta.
        Recurrence = recurrence;
        SetAlertLeadDays(alertLeadDays ?? DefaultAlertLead());

        if (CompetenceOf(anchorDueDate) is { } anchor)
            AnchorCompetence = anchor;

        Origin = ExpectationOrigin.Manual;

        // Só `Waiting` sai. Quem já se pronunciou é história, e história não se apaga por edição.
        _cycles.RemoveAll(c => c.Status == CycleStatus.Waiting && !IsOnSchedule(c.Competence));

        RescheduleWaitingCycles(occurredAt);

        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Abre todos os ciclos cuja hora de <strong>esperar</strong> já chegou. Chamado pela varredura.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A hora de abrir é <c>ObservedLeadDays</c> antes do vencimento, não
    /// <c>AlertLeadDays</c></strong> — o ciclo precisa estar aberto <em>quando a conta chega</em>,
    /// e a conta chega muito antes de a ausência dela virar aviso.
    /// </para>
    /// <para>
    /// <strong>Olha para a frente, não só para o mês corrente.</strong> A versão anterior derivava
    /// a competência do dia de hoje, e por isso o ciclo de setembro só podia nascer em setembro:
    /// uma conta que chega em agosto não tinha o que cumprir. Aqui a varredura anda pelas
    /// competências até a primeira cuja data de abertura ainda não chegou — como elas crescem em
    /// ordem, parar na primeira é seguro.
    /// </para>
    /// <para>
    /// <strong>Não abre competência cuja data de alerta já passou antes de a vigilância
    /// começar</strong> (<see cref="WatchingSince"/>): seria abrir para marcar como não cumprida
    /// no mesmo instante.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<ExpectationCycle> OpenDueCycles(DateOnly today, DateTime occurredAt)
    {
        if (!IsWatchingOn(today))
            return [];

        var opened = new List<ExpectationCycle>();
        var floor = DateOnly.FromDateTime(WatchingSince);
        var competence = new CompetencePeriod(today.Year, today.Month);

        for (var i = 0; i < MAX_COMPETENCES_AHEAD; i++, competence = competence.Next())
        {
            if (today < OpensAtFor(competence))
                break;

            if (!IsOnSchedule(competence) || CycleFor(competence) is not null)
                continue;

            if (AlertAtFor(competence) < floor)
                continue;

            opened.Add(OpenCycle(competence, occurredAt));
        }

        return opened;
    }

    /// <summary>
    /// Abre o ciclo de uma competência específica, sem conferir a cadência.
    /// </summary>
    /// <remarks>
    /// A cadência é conferida por <see cref="OpenDueCycles"/>, que é quem <em>prevê</em>. Este
    /// método também serve ao caminho oposto — o boleto que chegou antes de qualquer previsão e
    /// abre o próprio ciclo para ser cumprido —, e ali a competência não é previsão a conferir: é
    /// fato observado, e é ele que reancora a cadência no cumprimento.
    /// </remarks>
    public ExpectationCycle OpenCycle(CompetencePeriod competence, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(competence);
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
    /// <para>
    /// Média móvel, e não substituição, porque um mês atípico não pode redefinir a janela inteira
    /// — e não pode, tampouco, ser ignorado: concessionária muda calendário de faturamento.
    /// </para>
    /// <para>
    /// <strong><paramref name="arrivedOn"/> é quando o documento entrou no sistema, não o
    /// instante desta chamada.</strong> Medir o prazo pela hora do cumprimento encolheria o prazo
    /// observado a cada ciclo — e é ele que abre o ciclo seguinte, então o erro se realimentaria
    /// até a conta voltar a chegar antes de o ciclo existir.
    /// </para>
    /// </remarks>
    public void Fulfill(
        ExpectationCycleId cycleId,
        BillId billId,
        DateOnly actualDueDate,
        DateOnly arrivedOn,
        CaptureSourceId? arrivedThrough,
        DateTime occurredAt)
    {
        var cycle = RequireCycle(cycleId);

        cycle.Fulfill(billId, occurredAt);

        var lead = actualDueDate.DayNumber - arrivedOn.DayNumber;

        ObservationCount++;
        ExpectedDueDay = MovingAverage(ExpectedDueDay, actualDueDate.Day);
        SetObservedLeadDays(MovingAverage(ObservedLeadDays, lead < 0 ? 0 : lead));

        // A competência que de fato chegou é a melhor prova de onde a cadência está — é o que
        // absorve o calendário de faturamento que anda de mês sem ninguém reconfigurar nada.
        AnchorCompetence = cycle.Competence;

        // Por onde ela chegou vira o link acionável do próximo alerta de captura. Sem isto o
        // aviso "chegou e não consegui ler" não tem como saber a que conta o artefato pertence.
        if (arrivedThrough is not null)
            HintSourceId = arrivedThrough;

        // O default acompanha o que foi aprendido; antecedência escolhida à mão não é sobrescrita.
        if (Origin == ExpectationOrigin.Learned)
            SetAlertLeadDays(DefaultAlertLead());

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

        if (!cycle.RecordCaptureFailure(itemId, reason, occurredAt))
            return;

        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationCaptureFailedDomainEvent(
            Id, TenantId, cycle.Id, itemId, reason.Name, occurredAt));
    }

    /// <summary>
    /// O artefato que travava um ciclo foi resolvido sem virar boleto — o ciclo volta a esperar.
    /// </summary>
    /// <remarks>
    /// <strong>Sem isto o painel mentiria.</strong> Um item reprovado, reaberto ou descartado sai
    /// da fila de pendências, mas o ciclo continuaria apontando para ele como "resolva este item"
    /// — e alerta que aponta para trabalho já resolvido treina a pessoa a ignorar alerta tão bem
    /// quanto alerta indevido. Volta para <c>Waiting</c>, não para <c>Missing</c>: a conta ainda
    /// pode chegar, e a varredura decide de novo quando passar da data de alerta.
    /// </remarks>
    public void ClearCaptureFailure(CaptureItemId itemId, DateTime occurredAt)
    {
        var cleared = false;

        foreach (var cycle in _cycles)
            cleared |= cycle.ClearCaptureFailure(itemId, occurredAt);

        if (cleared)
            UpdatedAt = occurredAt;
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
    /// <remarks>
    /// <strong>É aqui que o escalonamento vira aviso.</strong> Até 2026-08-27 este método não
    /// emitia evento nenhum, e quem notificava era a transição para <c>Missing</c> — que acontece
    /// uma vez só por ciclo. O resultado é que dos quatro níveis apenas o primeiro chegava ao
    /// usuário: <c>Warning</c>, <c>Urgent</c> e <c>Overdue</c> ficavam gravados no agregado e
    /// nunca saíam. O evento nasce <em>dentro</em> da guarda de "um alerta por nível por ciclo",
    /// então escalonar não é o mesmo que repetir.
    /// </remarks>
    public bool TryRecordAlert(ExpectationCycleId cycleId, AlertLevel level, DateTime occurredAt)
    {
        if (level is null)
            throw BillExpectationErrors.AlertLevelRequired();

        var cycle = RequireCycle(cycleId);

        if (!cycle.TryRecordAlert(level, occurredAt))
            return false;

        UpdatedAt = occurredAt;

        AddDomainEvent(new BillExpectationAlertRaisedDomainEvent(
            Id,
            TenantId,
            cycle.Id,
            Label,
            level.Name,
            cycle.Competence.ToString(),
            cycle.ExpectedDueDate,
            cycle.MissReason?.Name,
            cycle.MissReason?.Arrived ?? false,
            cycle.BlockedByCaptureItemId?.Value,
            occurredAt));

        return true;
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

    /// <summary>
    /// Volta a vigiar. <strong>A vigilância recomeça agora</strong>, e não retroage à pausa: as
    /// competências que venceram durante ela não viram alerta de uma conta que ninguém observava.
    /// </summary>
    public void Resume(DateTime occurredAt)
    {
        PausedUntil = null;
        WatchingSince = occurredAt;
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
        WatchingSince = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>Carimba a passagem da varredura. Ver <see cref="LastSweptAt"/>.</summary>
    public void MarkSwept(DateTime occurredAt) => LastSweptAt = occurredAt;

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

    /// <summary>O ciclo de uma competência, se houver.</summary>
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
        ArgumentNullException.ThrowIfNull(competence);

        var daysInMonth = DateTime.DaysInMonth(competence.Year, competence.Month);

        return new DateOnly(competence.Year, competence.Month, Math.Min(ExpectedDueDay, daysInMonth));
    }

    /// <summary>
    /// Reposiciona os ciclos que ainda esperam, depois de o calendário mudar. Os que já se
    /// pronunciaram são recusados pelo próprio ciclo.
    /// </summary>
    private void RescheduleWaitingCycles(DateTime occurredAt)
    {
        foreach (var cycle in _cycles)
        {
            var dueDate = DueDateIn(cycle.Competence);
            cycle.Reschedule(dueDate, dueDate.AddDays(-AlertLeadDays), occurredAt);
        }
    }

    private static int MonthNumber(CompetencePeriod competence) => (competence.Year * 12) + competence.Month;

    /// <summary>
    /// A competência de um vencimento conhecido. Composta aqui, e não no chamador, porque o
    /// cadastro chega em primitivos pela borda HTTP e Value Object não se monta na Application.
    /// </summary>
    private static CompetencePeriod? CompetenceOf(DateOnly? dueDate)
        => dueDate is { } date ? new CompetencePeriod(date.Year, date.Month) : null;

    private ExpectationCycle RequireCycle(ExpectationCycleId cycleId)
        => _cycles.Find(c => c.Id == cycleId)
            ?? throw BillExpectationErrors.CycleNotFound(cycleId.Value);

    private void EnsureActive()
    {
        if (!IsActive)
            throw BillExpectationErrors.Inactive();
    }

    /// <summary>
    /// A antecedência derivada do prazo observado, já dentro do teto da recorrência.
    /// </summary>
    /// <remarks>
    /// O teto do prazo observado é 180 dias (conta mensal que chega dois meses antes existe), mas
    /// o alerta não pode passar de <c>IntervalDays - 1</c>. Sem o corte aqui, uma mensal com
    /// prazo observado de 28 dias derivava 30, e <see cref="SetAlertLeadDays"/> lançava
    /// <c>BLP.EXP05</c> de dentro de <see cref="Fulfill"/> — depois de o ciclo já ter sido
    /// cumprido em memória. A transação não salvava, o boleto que chegou nunca cumpria o ciclo,
    /// e a expectativa alertava "não chegou" sobre um boleto aprovado (auditoria 2026-08-28).
    /// Lê <see cref="Recurrence"/>, então só vale depois de ela estar atribuída.
    /// </remarks>
    private int DefaultAlertLead()
        => Math.Min(
            Math.Max(DEFAULT_MIN_ALERT_LEAD_DAYS, ObservedLeadDays + ALERT_LEAD_SLACK_DAYS),
            Recurrence.IntervalDays - 1);

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

    /// <summary>
    /// O prazo observado é <strong>corrigido</strong>, não recusado, quando sai da faixa.
    /// </summary>
    /// <remarks>
    /// Ele alimenta a média móvel a cada cumprimento, e um valor absurdo vindo de um vencimento
    /// mal lido não pode derrubar o cumprimento de um boleto que de fato chegou — o cumprimento é
    /// o dado bom da operação, e recusá-lo pelo ruído do calendário inverteria a prioridade.
    /// </remarks>
    private void SetObservedLeadDays(int value)
        => ObservedLeadDays = Math.Clamp(value, 0, MAX_OBSERVED_LEAD_DAYS);

    private void SetAlertLeadDays(int value)
    {
        var maximum = Recurrence.IntervalDays - 1;

        if (value < MIN_ALERT_LEAD_DAYS || value > maximum)
            throw BillExpectationErrors.InvalidAlertLead(maximum);

        AlertLeadDays = value;
    }
}
