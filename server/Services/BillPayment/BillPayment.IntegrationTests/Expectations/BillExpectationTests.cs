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

    public BillExpectationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // O cadastro grava a expectativa com a antecedência derivada do prazo observado — e é o
    // caminho obrigatório quando o tenant tem mais de uma conta do mesmo beneficiário.
    [Fact]
    public async Task Register_ShouldPersistWithTheDerivedAlertLead()
    {
        var payeeId = await SeedPayeeAsync();

        var response = await SendAsync(new RegisterBillExpectationCommand(
            Tenant.Value, payeeId.Value, "18502", "DAE — Água Americana L18502",
            nameof(Recurrence.Monthly), ExpectedDueDay: 10, ObservedLeadDays: 8, AlertLeadDays: null));

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

    // A varredura abre o ciclo quando entra na janela de alerta, e não antes: abrir cedo demais
    // encheria o painel de ciclos que ninguém pode resolver ainda.
    [Fact]
    public async Task Sweep_WhenTheAlertWindowOpens_ShouldOpenTheCycle()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await RegisterAsync(payeeId, "18502", dueDay: DateTime.UtcNow.Day);

        var response = await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        Assert.Contains(response.Outcome, new[] { "CycleOpened", "Missed", "Alerted" }, StringComparer.Ordinal);

        var stored = await LoadAsync(expectationId);
        Assert.Single(stored!.Cycles);
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

    // O painel separa as três filas porque a ação do usuário muda: buscar, resolver o item, ou
    // apenas se preparar. É o canal que funciona mesmo sem e-mail configurado.
    [Fact]
    public async Task ListPending_ShouldSeparateMissingFromDueSoon()
    {
        var payeeId = await SeedPayeeAsync();
        var expectationId = await SeedWithOverdueCycleAsync(payeeId);

        await SendAsync(new SweepBillExpectationCommand(Tenant.Value, expectationId.Value));

        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillExpectationQueries>();

        var view = await queries.ListPendingAsync(Tenant.Value, dueSoonWindowDays: 7);

        var pending = Assert.Single(view.Missing);
        Assert.Equal(expectationId.Value, pending.ExpectationId);
        Assert.Equal(nameof(MissReason.NeverArrived), pending.MissReason);
        Assert.False(pending.Arrived);
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
            nameof(Recurrence.Monthly), dueDay, ObservedLeadDays: 8, AlertLeadDays: null));

        return BillExpectationId.From(response.Id);
    }

    /// <summary>
    /// Uma expectativa com um ciclo cujo vencimento já passou, semeado direto pelo agregado — a
    /// varredura não tem como fabricar passado, e o domínio não lê relógio.
    /// </summary>
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
