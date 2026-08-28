namespace BillPayment.UnitTests.Expectations;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Expectations.Mothers;

/// <summary>
/// A separação entre o prazo que abre o ciclo e o prazo que dispara o alerta.
/// </summary>
/// <remarks>
/// Até 2026-08-27 os dois papéis moravam em <c>AlertLeadDays</c>, e o efeito era o oposto do
/// propósito do agregado: a conta chegava antes de o ciclo existir, não cumpria nada, e a
/// ausência dela virava alerta sobre um boleto capturado e aprovado.
/// </remarks>
public sealed class ExpectationLeadSplitTests
{
    /// <summary>
    /// Cadastro em 09/08/2026 — depois da data de alerta de agosto (08/08), de propósito.
    /// </summary>
    /// <remarks>
    /// O piso de boas-vindas então descarta a competência de agosto, e o cenário isola setembro.
    /// Cadastrando antes disso a varredura abriria os DOIS ciclos, e estaria certa: a conta de
    /// agosto também nunca chegou, e a vigilância já estava valendo.
    /// </remarks>
    private static readonly DateTime RegisteredAt = new(2026, 8, 9, 9, 0, 0, DateTimeKind.Utc);
    private static readonly BillId AnyBill = BillId.New();

    /// <summary>
    /// O caso relatado: vence dia 10, chega 20 dias antes, aviso pedido para 2 dias antes.
    /// </summary>
    private static BillExpectation ArrivesTwentyDaysEarly()
        => BillExpectationMother.Register(
            expectedDueDay: 10,
            observedLeadDays: 20,
            alertLeadDays: 2,
            occurredAt: RegisteredAt);

    // TESTE-ANCORA do defeito relatado: com vencimento 10/09, chegada 20 dias antes e aviso para
    // 2 dias antes, o ciclo passa a abrir na janela de CHEGADA (25 dias antes = 16/08) e não na
    // de alerta (08/09). Antes disso ele só nasceria em 08/09 — depois de a conta ter chegado.
    [Fact]
    public void OpenDueCycles_ShouldOpenOnTheArrivalWindow_NotTheAlertWindow()
    {
        var expectation = ArrivesTwentyDaysEarly();
        var september = new CompetencePeriod(2026, 9);

        Assert.Equal(new DateOnly(2026, 8, 16), expectation.OpensAtFor(september));
        Assert.Equal(new DateOnly(2026, 9, 8), expectation.AlertAtFor(september));

        var opened = expectation.OpenDueCycles(new DateOnly(2026, 8, 16), RegisteredAt);

        var cycle = Assert.Single(opened);
        Assert.Equal(september, cycle.Competence);
        Assert.Same(CycleStatus.Waiting, cycle.Status);
        Assert.Empty(cycle.Alerts);
    }

    // Na véspera da janela de chegada ainda não há ciclo: abrir antes encheria o painel de linhas
    // que ninguém pode resolver.
    [Fact]
    public void OpenDueCycles_BeforeTheArrivalWindow_ShouldOpenNothing()
        => Assert.Empty(ArrivesTwentyDaysEarly().OpenDueCycles(new DateOnly(2026, 8, 15), RegisteredAt));

    // O ciclo aberto cedo fica ESPERANDO até a data de alerta — não vira "não cumprido" só por
    // existir. É o que separa "estou de olho" de "reclame agora".
    [Fact]
    public void Sweep_BetweenArrivalAndAlert_ShouldKeepTheCycleWaiting()
    {
        var expectation = ArrivesTwentyDaysEarly();
        var cycle = expectation.OpenDueCycles(new DateOnly(2026, 8, 16), RegisteredAt).Single();

        var due = AlertLevel.DueOn(new DateOnly(2026, 9, 1), cycle.AlertAt, cycle.ExpectedDueDate);

        Assert.Null(due);
        Assert.Same(CycleStatus.Waiting, cycle.Status);
    }

    // Chegando a data de alerta (08/09) o nível devido é Warning — e não HeadsUp —, porque D-2
    // já cai dentro da faixa de D-3 do escalonamento.
    [Fact]
    public void AlertLevel_OnTheAlertDate_ShouldBeWarningWhenTheLeadIsShorterThanThreeDays()
    {
        var expectation = ArrivesTwentyDaysEarly();
        var cycle = expectation.OpenDueCycles(new DateOnly(2026, 8, 16), RegisteredAt).Single();

        Assert.Same(AlertLevel.Warning, AlertLevel.DueOn(new DateOnly(2026, 9, 8), cycle.AlertAt, cycle.ExpectedDueDate));
        Assert.Same(AlertLevel.Urgent, AlertLevel.DueOn(new DateOnly(2026, 9, 10), cycle.AlertAt, cycle.ExpectedDueDate));
        Assert.Same(AlertLevel.Overdue, AlertLevel.DueOn(new DateOnly(2026, 9, 11), cycle.AlertAt, cycle.ExpectedDueDate));
    }

    // TESTE-ANCORA do desfecho bom: o boleto que chega em 21/08 encontra o ciclo ABERTO e o
    // cumpre. Era este o caso que produzia o alerta falso — o ciclo ainda não existia ao chegar.
    [Fact]
    public void Match_WhenTheBillArrivesWithinTheArrivalWindow_ShouldFindTheCycle()
    {
        var expectation = ArrivesTwentyDaysEarly();
        expectation.OpenDueCycles(new DateOnly(2026, 8, 16), RegisteredAt);

        var match = ExpectationMatchingService.Match(
            [expectation], new DateOnly(2026, 9, 10), new DateOnly(2026, 8, 21));

        Assert.NotNull(match);
        Assert.Equal(expectation.Id, match.ExpectationId);
    }

    // A recorrência não-mensal abre ciclo SÓ nos meses da cadência. Sem a âncora, a varredura
    // abria um por mês para toda expectativa — e a anual acumulava onze Missing por ano, sendo
    // desativada pela regra do silêncio em três meses.
    [Fact]
    public void OpenDueCycles_Quarterly_ShouldSkipTheMonthsOutsideTheSchedule()
    {
        var expectation = BillExpectationMother.Register(
            recurrence: Recurrence.Quarterly,
            expectedDueDay: 10,
            observedLeadDays: 5,
            alertLeadDays: 3,
            occurredAt: RegisteredAt,
            anchorDueDate: new DateOnly(2026, 7, 10));

        Assert.True(expectation.IsOnSchedule(new CompetencePeriod(2026, 10)));
        Assert.False(expectation.IsOnSchedule(new CompetencePeriod(2026, 8)));
        Assert.False(expectation.IsOnSchedule(new CompetencePeriod(2026, 9)));

        // Agosto está fora da cadência: nada abre, mesmo com a data de abertura já vencida.
        Assert.Empty(expectation.OpenDueCycles(new DateOnly(2026, 8, 20), RegisteredAt));
    }

    // A cadência anda sozinha com o cumprimento: a competência que de fato chegou reancora a
    // fase, o que absorve calendário de faturamento que muda de mês sem ninguém reconfigurar.
    [Fact]
    public void Fulfill_ShouldReanchorTheScheduleOnTheCompetenceThatArrived()
    {
        var expectation = BillExpectationMother.Register(
            recurrence: Recurrence.Bimonthly, occurredAt: RegisteredAt,
            anchorDueDate: new DateOnly(2026, 7, 10));

        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), RegisteredAt);

        expectation.Fulfill(
            cycle.Id, AnyBill, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 2),
            arrivedThrough: null, RegisteredAt);

        Assert.Equal(new CompetencePeriod(2026, 8), expectation.AnchorCompetence);
        Assert.True(expectation.IsOnSchedule(new CompetencePeriod(2026, 10)));
    }

    // TESTE-ANCORA da média móvel: o prazo observado sai da data em que o documento CHEGOU, não
    // do instante do cumprimento. Medi-lo pela hora da chamada encolheria o prazo a cada ciclo —
    // e é ele que abre o ciclo seguinte, então o erro se realimentaria até a conta voltar a
    // chegar antes de o ciclo existir.
    [Fact]
    public void Fulfill_ShouldMeasureTheLeadFromTheArrivalDate_NotFromTheCallInstant()
    {
        var expectation = BillExpectationMother.Register(observedLeadDays: 20, occurredAt: RegisteredAt);
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), RegisteredAt);

        // Chegou 21 dias antes do vencimento; o cumprimento só foi processado no dia do vencimento.
        expectation.Fulfill(
            cycle.Id, AnyBill,
            actualDueDate: new DateOnly(2026, 8, 10),
            arrivedOn: new DateOnly(2026, 7, 20),
            arrivedThrough: null,
            occurredAt: new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(21, expectation.ObservedLeadDays);
    }

    // O prazo observado deixou de ser limitado pelo intervalo da recorrência: conta mensal que
    // chega dois meses antes é fato do arquivo real, e o teto antigo proibia justamente a
    // configuração que o agregado precisa suportar.
    [Fact]
    public void Register_MonthlyWithLeadLongerThanTheInterval_ShouldBeAccepted()
    {
        var expectation = BillExpectationMother.Register(
            observedLeadDays: 60, alertLeadDays: 5, occurredAt: RegisteredAt);

        Assert.Equal(60, expectation.ObservedLeadDays);
        Assert.Equal(65, expectation.OpenLeadDays);
    }

    // A antecedência do alerta nunca fica DEPOIS da abertura do ciclo: pedir aviso com mais
    // antecedência do que a conta chega faria o ciclo nascer já vencido de alerta.
    [Fact]
    public void OpenLeadDays_WhenTheAlertLeadIsLongerThanTheArrivalLead_ShouldFollowTheAlert()
    {
        var expectation = BillExpectationMother.Register(
            observedLeadDays: 2, alertLeadDays: 20, occurredAt: RegisteredAt);

        Assert.Equal(20, expectation.OpenLeadDays);
        Assert.True(expectation.OpensAtFor(new CompetencePeriod(2026, 9))
            <= expectation.AlertAtFor(new CompetencePeriod(2026, 9)));
    }

    // Piso de boas-vindas: competência cuja data de alerta já passou quando a expectativa nasceu
    // não abre — seria abrir para marcar como não cumprida no mesmo instante.
    [Fact]
    public void OpenDueCycles_WhenTheAlertDatePrecedesRegistration_ShouldSkipTheCompetence()
    {
        var registeredLate = new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc);
        var expectation = BillExpectationMother.Register(
            expectedDueDay: 10, observedLeadDays: 8, occurredAt: registeredLate);

        var opened = expectation.OpenDueCycles(new DateOnly(2026, 9, 20), registeredLate);

        Assert.DoesNotContain(opened, c => c.Competence.Equals(new CompetencePeriod(2026, 9)));
    }

    // O casamento resolve pela COMPETÊNCIA antes da janela de dias: vencimento 8, 10 ou 12 de
    // setembro descrevem a mesma conta de setembro, e o mês decide isso sem tolerância nenhuma.
    [Fact]
    public void Match_ShouldResolveByCompetence_EvenWhenTheDayIsOutsideTheTolerance()
    {
        var expectation = BillExpectationMother.Register(expectedDueDay: 10, occurredAt: RegisteredAt);
        expectation.OpenCycle(new CompetencePeriod(2026, 9), RegisteredAt);

        var match = ExpectationMatchingService.Match(
            [expectation], new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 1));

        Assert.NotNull(match);
    }

    // Fora da competência, a tolerância caiu para três dias — quinze existiam para compensar um
    // ciclo que abria tarde demais, e o ciclo deixou de abrir tarde.
    [Fact]
    public void Match_OutsideTheCompetence_ShouldOnlyBridgeTheMonthTurn()
    {
        var expectation = BillExpectationMother.Register(expectedDueDay: 30, occurredAt: RegisteredAt);
        expectation.OpenCycle(new CompetencePeriod(2026, 9), RegisteredAt);

        // Vence 30/09 e foi emitido como 01/10: um dia de distância, atravessa o mês.
        Assert.NotNull(ExpectationMatchingService.Match(
            [expectation], new DateOnly(2026, 10, 1), new DateOnly(2026, 9, 1)));

        // Já 15/10 é outra conta, e casar seria dar por cumprido o ciclo errado.
        Assert.Null(ExpectationMatchingService.Match(
            [expectation], new DateOnly(2026, 10, 15), new DateOnly(2026, 9, 1)));
    }

    // O boleto que chega antes de qualquer previsão tem a expectativa escolhida sem ambiguidade —
    // e com duas contas do mesmo beneficiário o serviço recusa, como no casamento comum.
    [Fact]
    public void SoleWatchingWithoutCycleFor_WithTwoCandidates_ShouldRefuseToChoose()
    {
        var first = BillExpectationMother.Register(accountReference: "A", occurredAt: RegisteredAt);
        var second = BillExpectationMother.Register(accountReference: "B", occurredAt: RegisteredAt);
        var competence = new CompetencePeriod(2026, 9);

        Assert.NotNull(ExpectationMatchingService.SoleWatchingWithoutCycleFor(
            [first], competence, new DateOnly(2026, 8, 21)));

        Assert.Null(ExpectationMatchingService.SoleWatchingWithoutCycleFor(
            [first, second], competence, new DateOnly(2026, 8, 21)));
    }

    // O artefato travado casa pela FONTE, porque não tem beneficiário nem vencimento com que
    // casar — e é esse elo que faz existir o alerta de "chegou e não consegui ler".
    [Fact]
    public void CaptureMatch_ShouldBindTheStuckArtifactToTheCycleByArrivalWindow()
    {
        var sourceId = CaptureSourceId.New();
        var expectation = BillExpectationMother.Register(
            expectedDueDay: 10, observedLeadDays: 20, alertLeadDays: 2,
            occurredAt: RegisteredAt, hintSourceId: sourceId);

        expectation.OpenDueCycles(new DateOnly(2026, 8, 16), RegisteredAt);

        var match = ExpectationCaptureMatchingService.Match(
            [expectation], new DateOnly(2026, 8, 21), new DateOnly(2026, 8, 21));

        Assert.NotNull(match);
        Assert.Equal(expectation.Id, match.ExpectationId);
    }

    // O motivo do ciclo é traduzido do estado em que o artefato travou, e é ele que decide qual
    // dos dois avisos sai — `Arrived` separa "vá buscar" de "resolva este item".
    [Theory]
    [InlineData(nameof(CaptureItemStatus.Locked), true)]
    [InlineData(nameof(CaptureItemStatus.LinkFailed), true)]
    [InlineData(nameof(CaptureItemStatus.Unrouted), true)]
    [InlineData(nameof(CaptureItemStatus.Unrecognized), true)]
    [InlineData(nameof(CaptureItemStatus.Failed), true)]
    public void ReasonFor_StuckStatuses_ShouldMapToAnArrivedReason(string status, bool arrived)
    {
        var reason = ExpectationCaptureMatchingService.ReasonFor(
            Enumeration.FromDisplayName<CaptureItemStatus>(status));

        Assert.NotNull(reason);
        Assert.Equal(arrived, reason.Arrived);
    }

    // Estado que NÃO aguarda resgate não descreve falha de captura nenhuma — sem esta guarda um
    // item promovido a boleto marcaria o ciclo como "não consegui ler".
    [Fact]
    public void ReasonFor_AResolvedStatus_ShouldBeNull()
        => Assert.Null(ExpectationCaptureMatchingService.ReasonFor(CaptureItemStatus.Promoted));

    // O ciclo travado volta a ESPERAR quando o artefato é resolvido sem virar boleto. Sem isto o
    // painel continuaria mandando resolver um item que já saiu da fila.
    [Fact]
    public void ClearCaptureFailure_ShouldReturnTheCycleToWaiting()
    {
        var itemId = CaptureItemId.New();
        var expectation = BillExpectationMother.Register(occurredAt: RegisteredAt);
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), RegisteredAt);

        expectation.RecordCaptureFailure(cycle.Id, itemId, MissReason.Locked, RegisteredAt);
        Assert.Same(CycleStatus.PartiallyCaptured, cycle.Status);

        expectation.ClearCaptureFailure(itemId, RegisteredAt);

        Assert.Same(CycleStatus.Waiting, cycle.Status);
        Assert.Null(cycle.BlockedByCaptureItemId);
        Assert.Null(cycle.MissReason);
    }

    // Registrar a MESMA falha do mesmo item de novo não muda nada e não emite aviso — é o que
    // torna o handler idempotente contra a reentrega do outbox.
    [Fact]
    public void RecordCaptureFailure_Repeated_ShouldNotRaiseASecondEvent()
    {
        var itemId = CaptureItemId.New();
        var expectation = BillExpectationMother.Register(occurredAt: RegisteredAt);
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), RegisteredAt);

        expectation.RecordCaptureFailure(cycle.Id, itemId, MissReason.Locked, RegisteredAt);
        expectation.PullDomainEvents();

        expectation.RecordCaptureFailure(cycle.Id, itemId, MissReason.Locked, RegisteredAt);

        Assert.Empty(expectation.PullDomainEvents());
    }

    // TESTE-ANCORA do escalonamento que notifica: cada nível emite o evento UMA vez. Enquanto o
    // aviso pendurava na transição para Missing — que acontece uma vez por ciclo —, os níveis
    // Warning, Urgent e Overdue eram gravados no agregado e nunca chegavam a ninguém.
    [Fact]
    public void TryRecordAlert_ShouldRaiseOneEventPerLevel()
    {
        var expectation = BillExpectationMother.Register(occurredAt: RegisteredAt);
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), RegisteredAt);
        expectation.PullDomainEvents();

        Assert.True(expectation.TryRecordAlert(cycle.Id, AlertLevel.Warning, RegisteredAt));
        Assert.False(expectation.TryRecordAlert(cycle.Id, AlertLevel.Warning, RegisteredAt));
        Assert.True(expectation.TryRecordAlert(cycle.Id, AlertLevel.Urgent, RegisteredAt));

        var raised = expectation.PullDomainEvents()
            .OfType<BillExpectationAlertRaisedDomainEvent>()
            .ToList();

        Assert.Equal(2, raised.Count);
        Assert.Equal([nameof(AlertLevel.Warning), nameof(AlertLevel.Urgent)], raised.ConvertAll(e => e.Level));
    }
}
