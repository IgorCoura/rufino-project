namespace BillPayment.UnitTests.Expectations;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Expectations.Mothers;

/// <summary>
/// A rede de segurança contra falha silenciosa: o que se espera receber e o aviso de não recebeu.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O modo de falha que estes testes protegem é o silêncio.</strong> Sem DDA nenhum canal
/// garante que a conta foi emitida, então automatizar a captura sem isto trocaria a conferência
/// manual — que ao menos falha de forma visível — por uma automação que falha calada, e cuja
/// primeira notícia seria a multa (ADR-014).
/// </para>
/// <para>
/// Metade deles cobre a defesa contra <strong>falso positivo</strong>, que é o outro jeito de o
/// mecanismo morrer: alerta indevido treina o usuário a ignorar alerta.
/// </para>
/// </remarks>
public class BillExpectationTests
{
    private static readonly DateTime OccurredAt = BillExpectationMother.DefaultOccurredAt;
    private static readonly BillId AnyBill = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));

    // O default da antecedência sai do prazo observado com folga, e nunca abaixo do piso: conta
    // que chega em cima da hora ainda precisa dar tempo de reagir.
    [Theory]
    [InlineData(8, 10)]  // 8 + 2 de folga
    [InlineData(0, 3)]   // piso
    [InlineData(1, 3)]   // piso ainda vence
    public void Register_WithoutAnExplicitLead_ShouldDeriveItFromTheObservedLead(int observed, int expected)
    {
        Assert.Equal(expected, BillExpectationMother.Register(observedLeadDays: observed).AlertLeadDays);
    }

    // Antecedência maior que o próprio intervalo faria o alerta de um ciclo nascer antes de o
    // anterior fechar — BLP.EXP05.
    [Fact]
    public void Register_WithALeadLongerThanTheRecurrence_ShouldThrow_BLP_EXP05()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BillExpectationMother.Register(recurrence: Recurrence.Monthly, alertLeadDays: 30));

        Assert.Equal("BLP.EXP05", ex.Id);
    }

    // Referência de conta ausente vira string vazia, nunca nulo: ela entra num índice único, e no
    // Postgres NULL não colide com NULL — duas expectativas sem referência passariam pelo banco.
    [Fact]
    public void Register_WithoutAnAccountReference_ShouldStoreAnEmptyStringNotNull()
    {
        Assert.Equal(string.Empty, BillExpectationMother.Register().AccountReference);
    }

    // Dois ciclos na mesma competência descreveriam a mesma conta duas vezes, e o segundo
    // alertaria sozinho depois de o primeiro ter sido cumprido — BLP.EXP02.
    [Fact]
    public void OpenCycle_Twice_ForTheSameCompetence_ShouldThrow_BLP_EXP02()
    {
        var (expectation, _) = BillExpectationMother.WithOpenCycle();

        var ex = Assert.Throws<DomainException>(
            () => expectation.OpenCycle(new CompetencePeriod(2026, 8), OccurredAt));

        Assert.Equal("BLP.EXP02", ex.Id);
    }

    // A data de alerta é o vencimento menos a antecedência APRENDIDA — não uma regra fixa.
    [Fact]
    public void OpenCycle_ShouldSetTheAlertDateFromTheLearnedLead()
    {
        var (_, cycle) = BillExpectationMother.WithOpenCycle();

        Assert.Equal(new DateOnly(2026, 8, 10), cycle.ExpectedDueDate);
        Assert.Equal(new DateOnly(2026, 7, 31), cycle.AlertAt);
    }

    // Dia 31 numa competência de trinta dias não existe: sem o ajuste, o job pararia de abrir
    // ciclo justamente para as contas que vencem no fim do mês.
    [Fact]
    public void OpenCycle_WhenTheDueDayExceedsTheMonth_ShouldClampToItsLastDay()
    {
        var expectation = BillExpectationMother.Register(expectedDueDay: 31);

        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 9), OccurredAt);

        Assert.Equal(new DateOnly(2026, 9, 30), cycle.ExpectedDueDate);
    }

    // Marcar como não cumprido antes da data de alerta transformaria a expectativa em fonte de
    // falso positivo — BLP.EXP04.
    [Fact]
    public void MarkMissing_BeforeTheAlertDate_ShouldThrow_BLP_EXP04()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();

        var ex = Assert.Throws<DomainException>(() => expectation.MarkMissing(
            cycle.Id, MissReason.NeverArrived, new DateOnly(2026, 7, 20), OccurredAt));

        Assert.Equal("BLP.EXP04", ex.Id);
    }

    // O cumprimento fecha o ciclo e aprende: dia de vencimento e prazo observado são reajustados.
    [Fact]
    public void Fulfill_ShouldCloseTheCycleAndLearnFromTheActualDueDate()
    {
        var expectation = BillExpectationMother.Learned(observationCount: 3, expectedDueDay: 10);
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, 8), OccurredAt);

        expectation.Fulfill(cycle.Id, AnyBill, new DateOnly(2026, 8, 14), OccurredAt);

        Assert.Same(CycleStatus.Fulfilled, cycle.Status);
        Assert.Equal(AnyBill, cycle.FulfilledByBillId);
        Assert.Equal(4, expectation.ObservationCount);

        // Média móvel: um mês atípico ajusta a janela, não a redefine.
        Assert.InRange(expectation.ExpectedDueDay, 11, 13);
    }

    // Cumprir emite o evento que fecha o ciclo para quem escuta.
    [Fact]
    public void Fulfill_ShouldRaiseTheFulfilledEvent()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();
        expectation.PullDomainEvents();

        expectation.Fulfill(cycle.Id, AnyBill, new DateOnly(2026, 8, 10), OccurredAt);

        var raised = Assert.Single(expectation.PullDomainEvents());
        var fulfilled = Assert.IsType<BillExpectationFulfilledDomainEvent>(raised);
        Assert.Equal(cycle.Id, fulfilled.CycleId);
        Assert.Equal(AnyBill, fulfilled.BillId);
    }

    // Chegou e não deu para ler é desfecho DIFERENTE de não chegou: o documento existe, o sistema
    // sabe o que falta, e o aviso leva ao item resolvível.
    [Fact]
    public void RecordCaptureFailure_ShouldMarkPartiallyCapturedAndPointToTheItem()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();
        var itemId = CaptureItemId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c9"));

        expectation.RecordCaptureFailure(cycle.Id, itemId, MissReason.Locked, OccurredAt);

        Assert.Same(CycleStatus.PartiallyCaptured, cycle.Status);
        Assert.Equal(itemId, cycle.BlockedByCaptureItemId);
        Assert.True(cycle.MissReason!.Arrived);

        // Não conta como observação: nada se aprendeu sobre o calendário.
        Assert.Equal(0, expectation.ObservationCount);
    }

    // Um alerta por nível por ciclo. Repetir o mesmo nível é o caminho mais curto para o usuário
    // aprender a ignorar alerta.
    [Fact]
    public void TryRecordAlert_ForTheSameLevelTwice_ShouldRecordOnlyOnce()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();

        Assert.True(expectation.TryRecordAlert(cycle.Id, AlertLevel.HeadsUp, OccurredAt));
        Assert.False(expectation.TryRecordAlert(cycle.Id, AlertLevel.HeadsUp, OccurredAt.AddDays(1)));

        Assert.Single(cycle.Alerts);
    }

    // Níveis diferentes do mesmo ciclo escalam normalmente.
    [Fact]
    public void TryRecordAlert_ForDifferentLevels_ShouldRecordEachOne()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();

        expectation.TryRecordAlert(cycle.Id, AlertLevel.HeadsUp, OccurredAt);
        expectation.TryRecordAlert(cycle.Id, AlertLevel.Warning, OccurredAt.AddDays(7));
        expectation.TryRecordAlert(cycle.Id, AlertLevel.Urgent, OccurredAt.AddDays(10));

        Assert.Equal(3, cycle.Alerts.Count);
    }

    // Dispensar um ciclo não desativa a expectativa — é a defesa mais barata contra falso
    // positivo, e sem ela a única saída seria desativar tudo.
    [Fact]
    public void Waive_ShouldCloseOnlyTheCycleAndKeepTheExpectationActive()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();

        expectation.Waive(cycle.Id, BillExpectationMother.DefaultUser, "imóvel desocupado", OccurredAt);

        Assert.Same(CycleStatus.Waived, cycle.Status);
        Assert.True(expectation.IsActive);
        Assert.Equal(BillExpectationMother.DefaultUser, cycle.WaivedBy);
    }

    // Ciclo fechado não aceita mais operação — BLP.EXP03.
    [Fact]
    public void Fulfill_OnAWaivedCycle_ShouldThrow_BLP_EXP03()
    {
        var (expectation, cycle) = BillExpectationMother.WithOpenCycle();
        expectation.Waive(cycle.Id, BillExpectationMother.DefaultUser, reason: null, OccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => expectation.Fulfill(cycle.Id, AnyBill, new DateOnly(2026, 8, 10), OccurredAt));

        Assert.Equal("BLP.EXP03", ex.Id);
    }

    // Pausa suspende o monitoramento até a data, e ele volta sozinho depois.
    [Fact]
    public void Pause_ShouldStopWatchingUntilTheDatePasses()
    {
        var expectation = BillExpectationMother.Register();

        expectation.Pause(new DateOnly(2026, 12, 31), OccurredAt);

        Assert.False(expectation.IsWatchingOn(new DateOnly(2026, 9, 1)));
        Assert.True(expectation.IsWatchingOn(new DateOnly(2027, 1, 1)));
    }

    // Expectativa desativada não abre ciclo — BLP.EXP13.
    [Fact]
    public void OpenCycle_WhenDeactivated_ShouldThrow_BLP_EXP13()
    {
        var expectation = BillExpectationMother.Register();
        expectation.Deactivate("imóvel vendido", OccurredAt);

        var ex = Assert.Throws<DomainException>(
            () => expectation.OpenCycle(new CompetencePeriod(2026, 8), OccurredAt));

        Assert.Equal("BLP.EXP13", ex.Id);
    }

    // Três ciclos seguidos sem cumprimento e sem reação do usuário: a expectativa morreu, e
    // continuar alertando treinaria a pessoa a ignorar alerta.
    [Fact]
    public void ShouldDeactivateForSilence_AfterThreeConsecutiveMisses_ShouldBeTrue()
    {
        var expectation = BillExpectationMother.Register();

        foreach (var month in (int[])[5, 6, 7])
            MissCycle(expectation, month);

        Assert.True(expectation.ShouldDeactivateForSilence());
    }

    // Um cumprimento no meio zera a sequência: prova que a conta continua existindo.
    [Fact]
    public void ShouldDeactivateForSilence_WhenACycleWasFulfilledInBetween_ShouldBeFalse()
    {
        var expectation = BillExpectationMother.Register();

        MissCycle(expectation, 5);

        var fulfilled = expectation.OpenCycle(new CompetencePeriod(2026, 6), OccurredAt);
        expectation.Fulfill(fulfilled.Id, AnyBill, new DateOnly(2026, 6, 10), OccurredAt);

        MissCycle(expectation, 7);

        Assert.False(expectation.ShouldDeactivateForSilence());
    }

    // Dois não bastam — o teto é três, e antes dele o usuário ainda pode estar só demorando.
    [Fact]
    public void ShouldDeactivateForSilence_AfterTwoMisses_ShouldBeFalse()
    {
        var expectation = BillExpectationMother.Register();

        foreach (var month in (int[])[6, 7])
            MissCycle(expectation, month);

        Assert.False(expectation.ShouldDeactivateForSilence());
    }

    // Um ciclo dispensado quebra a sequência: o usuário reagiu, e a expectativa não morreu.
    [Fact]
    public void ShouldDeactivateForSilence_WhenACycleWasWaived_ShouldBeFalse()
    {
        var expectation = BillExpectationMother.Register();

        MissCycle(expectation, 5);

        var waived = expectation.OpenCycle(new CompetencePeriod(2026, 6), OccurredAt);
        expectation.Waive(waived.Id, BillExpectationMother.DefaultUser, reason: null, OccurredAt);

        MissCycle(expectation, 7);

        Assert.False(expectation.ShouldDeactivateForSilence());
    }

    // Aprender avisa: criar em silêncio faria a primeira notícia da existência da expectativa ser
    // um alerta que o usuário não pediu.
    [Fact]
    public void Learn_ShouldRaiseTheLearnedEventSoTheUserIsTold()
    {
        var expectation = BillExpectationMother.Learned();

        var raised = Assert.Single(expectation.PullDomainEvents());
        var learned = Assert.IsType<BillExpectationLearnedDomainEvent>(raised);

        Assert.Equal(BillExpectationMother.DefaultLabel, learned.Label);
        Assert.Same(ExpectationOrigin.Learned, expectation.Origin);
    }

    private static void MissCycle(BillExpectation expectation, int month)
    {
        var cycle = expectation.OpenCycle(new CompetencePeriod(2026, month), OccurredAt);

        expectation.MarkMissing(
            cycle.Id, MissReason.NeverArrived, cycle.ExpectedDueDate.AddDays(1), OccurredAt);
    }
}
