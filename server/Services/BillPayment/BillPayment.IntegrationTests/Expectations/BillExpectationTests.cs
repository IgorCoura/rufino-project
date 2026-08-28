namespace BillPayment.IntegrationTests.Expectations;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.Expectations;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A rede de segurança de ponta a ponta: o ciclo abre, alerta, e é cumprido ou dispensado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O que se testa aqui é a falha SILENCIOSA.</strong> Sem DDA nenhum canal garante que a
/// conta foi emitida; a automação da captura, sozinha, troca uma conferência manual que falha de
/// forma visível por uma que falha calada (ADR-014).
/// </para>
/// <para>
/// A varredura é dirigida pelo comando, nunca pelo worker: o job vem <strong>ligado</strong> por
/// padrão em produção — ao contrário da captura — e por isso a fábrica de teste o desliga
/// explicitamente.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillExpectationTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a7"));
    private static readonly DateTime OccurredAt = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    private const string PayeeCnpj = "11444777000161";

    /// <summary>
    /// Os desfechos que provam que a varredura agiu. Qual deles sai depende do dia em que o
    /// teste roda em relação ao vencimento — o domínio não lê relógio, mas o job lê.
    /// </summary>
    private static readonly string[] SweepDidSomething = ["CycleOpened", "Missed", "Alerted"];

    public BillExpectationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // O cadastro grava a expectativa com a antecedência derivada do prazo observado — e é o
    // caminho obrigatório quando o tenant tem mais de uma conta do mesmo beneficiário.
    [Fact]
    public async Task Register_ShouldPersistWithTheDerivedAlertLead()
    {
        var payeeId = await SeedPayeeAsync();

        var response = await SendAsync(new RegisterBillExpectationCommand(
            Tenant.Value, payeeId.Value, "18502", "DAE — Água Americana L18502",
            nameof(Recurrence.Monthly), ExpectedDueDay: 10, ObservedLeadDays: 8, AlertLeadDays: null, FirstDueDate: null, HintSourceId: null));

        Assert.Equal(10, response.AlertLeadDays);

        var stored = await LoadAsync(BillExpectationId.From(response.Id));
        Assert.Equal("18502", stored!.AccountReference);
        Assert.Same(ExpectationOrigin.Manual, stored.Origin);
        Assert.True(stored.IsActive);
    }

    // Duas contas do MESMO beneficiário coexistem porque a referência entra na chave — é o caso
    // medido no arquivo real: quatro instalações da EDP, três do DAE.
    [Fact]
    public async Task Register_TwoAccountsOfTheSamePayee_ShouldBothBeAccepted()
    {
        var payeeId = await SeedPayeeAsync();

        await RegisterAsync(payeeId, "18502", dueDay: 10);
        await RegisterAsync(payeeId, "2748", dueDay: 20);

        var count = await ExecuteDbContextAsync(db =>
            db.BillExpectations.CountAsync(e => e.TenantId == Tenant));

        Assert.Equal(2, count);
    }

    // E a mesma referência duas vezes é recusada — BLP.EXP01.
    [Fact]
    public async Task Register_TheSameAccountTwice_ShouldThrow_BLP_EXP01()
    {
        var payeeId = await SeedPayeeAsync();
        await RegisterAsync(payeeId, "18502", dueDay: 10);

        var ex = await Assert.ThrowsAsync<DomainException>(() => RegisterAsync(payeeId, "18502", dueDay: 10));

        Assert.Equal("BLP.EXP01", ex.Id);
    }

    // A varredura abre o ciclo quando entra na janela de CHEGADA — o prazo observado, não a
    // antecedência do alerta. A vigilância é retroagida porque o piso de boas-vindas (o teste
    // seguinte) impediria uma expectativa recém-cadastrada de abrir ciclo cujo alerta já passou.
    [Fact]
    public async Task Sweep_WhenTheArrivalWindowOpens_ShouldOpenTheCycle()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: DateTime.UtcNow.Day);
        await BackdateWatchingSinceAsync(expectationId, days: 60);

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Contains(response.Outcome, SweepDidSomething, StringComparer.Ordinal);

        var stored = await LoadAsync(expectationId);
        Assert.Single(stored!.Cycles);
    }

    // TESTE-ANCORA do piso de boas-vindas: expectativa cadastrada HOJE, cuja data de alerta deste
    // mês já passou, NÃO abre ciclo. Sem esta guarda a varredura o abriria só para marcá-lo como
    // não cumprido no mesmo instante — um alerta falso sobre uma conta que ninguém pediu para
    // vigiar, que é a classe de erro que treina a pessoa a ignorar alerta.
    [Fact]
    public async Task Sweep_WhenTheAlertDateAlreadyPassedAtRegistration_ShouldNotOpenACycle()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: DateTime.UtcNow.Day);

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Equal("Idle", response.Outcome);
        Assert.Empty((await LoadAsync(expectationId))!.Cycles);
    }

    // Passou do vencimento sem cumprimento: o ciclo vira Missing e o alerta é REGISTRADO no
    // agregado — é o registro, e não o canal de envio, que sustenta o painel de pendências.
    [Fact]
    public async Task Sweep_WhenTheDueDatePassedWithNothingCaptured_ShouldMarkMissingAndRecordTheAlert()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Equal("Missed", response.Outcome);

        // A varredura também abre o ciclo do mês corrente: o atrasado é o de julho, e é sobre
        // ele que a varredura age — o ciclo vencido continua sendo o que importa mesmo depois de
        // o mês virar.
        var cycle = OverdueCycleOf(await LoadAsync(expectationId));

        Assert.Same(CycleStatus.Missing, cycle.Status);
        Assert.Same(MissReason.NeverArrived, cycle.MissReason);
        Assert.False(cycle.MissReason!.Arrived);
        Assert.NotEmpty(cycle.Alerts);
    }

    // Varrer de novo no mesmo dia NÃO repete o alerta — repetir nível é o caminho mais curto
    // para o usuário aprender a ignorar alerta.
    [Fact]
    public async Task Sweep_TwiceOnTheSameDay_ShouldNotRepeatTheAlert()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));
        await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Single(OverdueCycleOf(await LoadAsync(expectationId)).Alerts);
    }

    // Três ciclos seguidos não cumpridos desativam a expectativa: silêncio do usuário é sinal de
    // que ela morreu, e continuar alertando destruiria o mecanismo.
    [Fact]
    public async Task Sweep_AfterThreeConsecutiveMisses_ShouldDeactivateTheExpectation()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithConsecutiveMissesAsync(payeeId, misses: 3);

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Equal("Deactivated", response.Outcome);

        var stored = await LoadAsync(expectationId);
        Assert.False(stored!.IsActive);
        Assert.Equal("silence_after_consecutive_misses", stored.DeactivationReason);
    }

    // Dispensar um ciclo o fecha sem desativar a expectativa — a defesa mais barata contra falso
    // positivo, e sem ela a única saída para um mês atípico seria desativar tudo.
    [Fact]
    public async Task WaiveCycle_ShouldCloseTheCycleAndKeepTheExpectationActive()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);
        var cycleId = (await LoadAsync(expectationId))!.Cycles.First().Id;

        await SendAsync(new WaiveExpectationCycleCommand(
            Tenant.Value, expectationId.Value, cycleId.Value, Decider.Value, "obra parada"));

        var stored = await LoadAsync(expectationId);
        Assert.Same(CycleStatus.Waived, stored!.Cycles.First().Status);
        Assert.True(stored.IsActive);
    }

    // Pausar suspende o monitoramento sem apagar o histórico de ciclos.
    [Fact]
    public async Task AlterWatch_WhenPaused_ShouldStopWatchingWithoutLosingCycles()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await SendAsync(new AlterBillExpectationWatchCommand(
            Tenant.Value, expectationId.Value, IsActive: true,
            PausedUntil: new DateOnly(2027, 12, 31), Reason: null));

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Equal("Idle", response.Outcome);
        Assert.Single((await LoadAsync(expectationId))!.Cycles);
    }

    // O painel separa as filas porque a ação do usuário muda: buscar, resolver o item, ou apenas
    // se preparar. É o canal que funciona mesmo sem e-mail configurado.
    //
    // A conta não cumprida cujo vencimento JÁ PASSOU vai para `Overdue`, não para `Missing`: ali
    // há encargos correndo e a ação deixa de ser "ainda dá tempo de buscar". Misturar as duas faz
    // a vencida se perder no meio das outras, que é como uma rede de segurança deixa de ser lida.
    [Fact]
    public async Task ListPending_ShouldSeparateOverdueFromMissingAndDueSoon()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillExpectationQueries>();

        var view = await queries.ListPendingAsync(Tenant.Value, dueSoonWindowDays: 7);

        var pending = Assert.Single(view.Overdue);
        Assert.Equal(expectationId.Value, pending.ExpectationId);
        Assert.Equal(nameof(MissReason.NeverArrived), pending.MissReason);
        Assert.False(pending.Arrived);
        Assert.True(pending.IsOverdue);

        // A fila de "não chegou, mas ainda não venceu" fica vazia: o único ciclo não cumprido já
        // passou do vencimento.
        Assert.Empty(view.Missing);
        Assert.Empty(view.CaptureFailed);
    }

    // Chegou e não deu para ler é a OUTRA fila, e ela carrega o ponteiro do item resolvível —
    // é o alerta mais valioso, porque o sistema já tem o documento.
    [Fact]
    public async Task ListPending_WhenTheDocumentArrivedButCouldNotBeRead_ShouldLandInCaptureFailed()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);
        var itemId = Domain.CaptureItems.CaptureItemId.New();

        await ExecuteDbContextAsync(async db =>
        {
            var expectation = await db.BillExpectations
                .Include(e => e.Cycles)
                .FirstAsync(e => e.Id == expectationId);

            expectation.RecordCaptureFailure(
                expectation.Cycles.First().Id, itemId, MissReason.Locked, OccurredAt);

            await db.SaveEntitiesAsync();
        });

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillExpectationQueries>();

        var view = await queries.ListPendingAsync(Tenant.Value, dueSoonWindowDays: 7);

        var pending = Assert.Single(view.CaptureFailed);
        Assert.True(pending.Arrived);
        Assert.Equal(itemId.Value, pending.BlockedByCaptureItemId);
        Assert.Empty(view.Missing);
    }

    // Round-trip do agregado inteiro pelo banco: ciclos, alertas em jsonb e a competência
    // achatada em coluna escalar precisam voltar iguais.
    [Fact]
    public async Task Persistence_ShouldRoundTripCyclesAndAlerts()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        var cycle = OverdueCycleOf(await LoadAsync(expectationId));

        Assert.Equal(new CompetencePeriod(2026, 7), cycle.Competence);
        Assert.Single(cycle.Alerts);
        Assert.Same(AlertLevel.Overdue, cycle.Alerts.First().Level);
    }

    // A edicao grava os campos novos e sobrevive ao round-trip pelo banco.
    [Fact]
    public async Task Edit_ShouldReplaceEveryEditableFieldAndPersist()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await SendAsync(new EditBillExpectationCommand(
            Tenant.Value, expectationId.Value, "2748", "DAE - Agua Americana L2748",
            nameof(Recurrence.Bimonthly), ExpectedDueDay: 25, ObservedLeadDays: 12, AlertLeadDays: 15, FirstDueDate: null));

        var stored = await LoadAsync(expectationId);

        Assert.Equal("2748", stored!.AccountReference);
        Assert.Equal("DAE - Agua Americana L2748", stored.Label);
        Assert.Same(Recurrence.Bimonthly, stored.Recurrence);
        Assert.Equal(25, stored.ExpectedDueDay);
        Assert.Equal(12, stored.ObservedLeadDays);
        Assert.Equal(15, stored.AlertLeadDays);
    }

    // O beneficiario NAO e editavel: nao entra no comando, entao ele permanece o que era. Quem
    // precisa trocar exclui e cadastra de novo.
    [Fact]
    public async Task Edit_ShouldNeverChangeThePayee()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await EditAsync(expectationId, "2748", dueDay: 25);

        Assert.Equal(payeeId, (await LoadAsync(expectationId))!.PayeeId);
    }

    // TESTE-ANCORA do parametro `excluding`: editar sem mexer na referencia nao pode colidir com a
    // propria linha. Sem ele o indice unico do banco devolveria erro cru no lugar do BLP.EXP01 -
    // ou seja, editar so o rotulo ficaria impossivel.
    [Fact]
    public async Task Edit_KeepingTheSameAccountReference_ShouldNotCollideWithItself()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await EditAsync(expectationId, "18502", dueDay: 10, label: "DAE - rotulo corrigido");

        Assert.Equal("DAE - rotulo corrigido", (await LoadAsync(expectationId))!.Label);
    }

    // Mas mudar a referencia para a de uma conta IRMA do mesmo beneficiario colide - BLP.EXP01. E
    // a mesma invariante do cadastro, conferida do lado da edicao.
    [Fact]
    public async Task Edit_ToAnAccountReferenceOfASibling_ShouldThrow_BLP_EXP01()
    {
        var payeeId = await SeedPayeeAsync();
        await RegisterAsync(payeeId, "18502", dueDay: 10);
        var second = await RegisterAsync(payeeId, "2748", dueDay: 20);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => EditAsync(second, "18502", dueDay: 20));

        Assert.Equal("BLP.EXP01", ex.Id);
    }

    // A edicao reposiciona o ciclo que ainda espera, e a coleção owned volta do banco com as datas
    // novas - e onde um owned mal rastreado apareceria.
    [Fact]
    public async Task Edit_ShouldRescheduleTheWaitingCycleInTheDatabase()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await EditAsync(expectationId, "18502", dueDay: 25, alertLeadDays: 5);

        var cycle = OverdueCycleOf(await LoadAsync(expectationId));

        Assert.Equal(new DateOnly(2026, 7, 25), cycle.ExpectedDueDate);
        Assert.Equal(new DateOnly(2026, 7, 20), cycle.AlertAt);
    }

    // Expectativa de outro tenant nao existe para quem edita - BLP.EXP00, nunca uma mensagem que
    // confirme a existencia dela.
    [Fact]
    public async Task Edit_OfAnotherTenant_ShouldThrow_BLP_EXP00()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        var ex = await Assert.ThrowsAsync<DomainException>(() => SendAsync(
            new EditBillExpectationCommand(
                Guid.NewGuid(), expectationId.Value, "18502", "Conta alheia",
                nameof(Recurrence.Monthly), ExpectedDueDay: 10, ObservedLeadDays: 8, AlertLeadDays: null, FirstDueDate: null)));

        Assert.Equal("BLP.EXP00", ex.Id);
    }

    // TESTE-ANCORA da exclusao: a expectativa some E os ciclos vao junto, porque sao colecao owned
    // da raiz. Linha orfa em bill_expectation_cycles seria historico de uma conta que nao existe.
    [Fact]
    public async Task Delete_ShouldRemoveTheExpectationAndItsCycles()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        Assert.NotEqual(0, await CountCyclesAsync(expectationId));

        await SendAsync(new DeleteBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Null(await LoadAsync(expectationId));
        Assert.Equal(0, await CountCyclesAsync(expectationId));
    }

    // Excluir e recadastrar com o mesmo beneficiario e a mesma referencia funciona - e o caminho
    // que substitui a edicao de beneficiario.
    [Fact]
    public async Task Delete_ThenRegisterTheSameAccountAgain_ShouldBeAccepted()
    {
        var payeeId = await SeedPayeeAsync();
        var first = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await SendAsync(new DeleteBillExpectationCommand(Tenant.Value, first.Value));
        var second = await RegisterAsync(payeeId, "18502", dueDay: 10);

        Assert.NotEqual(first, second);
        Assert.NotNull(await LoadAsync(second));
    }

    // Excluir expectativa de outro tenant nao alcanca nada - BLP.EXP00, e a linha continua no banco.
    [Fact]
    public async Task Delete_OfAnotherTenant_ShouldThrow_BLP_EXP00AndKeepTheRow()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        var ex = await Assert.ThrowsAsync<DomainException>(() => SendAsync(
            new DeleteBillExpectationCommand(Guid.NewGuid(), expectationId.Value)));

        Assert.Equal("BLP.EXP00", ex.Id);
        Assert.NotNull(await LoadAsync(expectationId));
    }

    private Task<int> CountCyclesAsync(BillExpectationId expectationId)
        => ExecuteDbContextAsync(db => db.BillExpectations
            .AsNoTracking()
            .Where(e => e.Id == expectationId)
            .SelectMany(e => e.Cycles)
            .CountAsync());

    private Task<EditBillExpectationResponse> EditAsync(
        BillExpectationId expectationId,
        string reference,
        int dueDay,
        string? label = null,
        int? alertLeadDays = null)
        => SendAsync(new EditBillExpectationCommand(
            Tenant.Value, expectationId.Value, reference, label ?? $"Conta {reference}",
            nameof(Recurrence.Monthly), dueDay, ObservedLeadDays: 8, AlertLeadDays: alertLeadDays, FirstDueDate: null));

    /// <summary>
    /// O ciclo semeado como atrasado (julho/2026). A varredura abre também o do mês corrente, e
    /// é sobre o mais antigo em aberto que ela age.
    /// </summary>
    private static ExpectationCycle OverdueCycleOf(BillExpectation? expectation)
        => expectation!.Cycles.First(c => c.Competence.Equals(new CompetencePeriod(2026, 7)));

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> command)
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(command);
    }

    private Task<BillExpectation?> LoadAsync(BillExpectationId id)
        => ExecuteDbContextAsync(db => db.BillExpectations
            .AsNoTracking()
            .Include(e => e.Cycles)
            .FirstOrDefaultAsync(e => e.Id == id));

    private async Task<BillExpectationId> RegisterAsync(PayeeId payeeId, string reference, int dueDay)
    {
        var response = await SendAsync(new RegisterBillExpectationCommand(
            Tenant.Value, payeeId.Value, reference, $"Conta {reference}",
            nameof(Recurrence.Monthly), dueDay, ObservedLeadDays: 8, AlertLeadDays: null, FirstDueDate: null, HintSourceId: null));

        return BillExpectationId.From(response.Id);
    }

    /// <summary>
    /// Uma expectativa com um ciclo cujo vencimento já passou, semeado direto pelo agregado — a
    /// varredura não tem como fabricar passado, e o domínio não lê relógio.
    /// </summary>
    /// <summary>
    /// Retroage o início da vigilância, para exercitar a varredura de uma expectativa que já
    /// existia — em vez de uma recém-cadastrada, que o piso de boas-vindas protege.
    /// </summary>
    /// <remarks>
    /// Por SQL porque <c>WatchingSince</c> não tem setter público, e não deve ter: quem o move é
    /// o próprio agregado, ao nascer e ao retomar de uma pausa.
    /// </remarks>
    private async Task BackdateWatchingSinceAsync(BillExpectationId expectationId, int days)
        => await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.bill_expectations SET watching_since = watching_since - INTERVAL '{0} days' WHERE id = '{1}'"
                .Replace("{0}", days.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{1}", expectationId.Value.ToString(), StringComparison.Ordinal)));

    private async Task<BillExpectationId> SeedWithOverdueCycleAsync(PayeeId payeeId)
    {
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await ExecuteDbContextAsync(async db =>
        {
            var expectation = await db.BillExpectations
                .Include(e => e.Cycles)
                .FirstAsync(e => e.Id == expectationId);

            expectation.OpenCycle(new CompetencePeriod(2026, 7), OccurredAt);
            await db.SaveEntitiesAsync();
        });

        return expectationId;
    }

    private async Task<BillExpectationId> SeedWithConsecutiveMissesAsync(PayeeId payeeId, int misses)
    {
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: 10);

        await ExecuteDbContextAsync(async db =>
        {
            var expectation = await db.BillExpectations
                .Include(e => e.Cycles)
                .FirstAsync(e => e.Id == expectationId);

            for (var month = 1; month <= misses; month++)
            {
                var cycle = expectation.OpenCycle(new CompetencePeriod(2026, month), OccurredAt);
                expectation.MarkMissing(
                    cycle.Id, MissReason.NeverArrived, cycle.ExpectedDueDate.AddDays(1), OccurredAt);
            }

            await db.SaveEntitiesAsync();
        });

        return expectationId;
    }

    private Task<PayeeId> SeedPayeeAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var payee = Payee.Register(
                Tenant, "CONCESSIONARIA EXEMPLO SA", TaxId.Parse(PayeeCnpj), AmountPolicy.Unbounded(), OccurredAt);

            await db.Payees.AddAsync(payee);
            await db.SaveEntitiesAsync();
            return payee.Id;
        });
}
