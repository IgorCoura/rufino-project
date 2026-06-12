namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;
using EconomicCore.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Cenários reais de locação cobrindo as variações de valor: aluguéis curto/médio/longo, com/sem condomínio,
/// pagamento bundled, reajustes anual e semestral, rescisão antecipada e seguro como contrato separado.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RentScenariosTests : BaseIntegrationTest
{
    public RentScenariosTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private sealed record Row(Guid Id, string Direction, string Purpose, int Year, int Month, decimal Amount, string Status);

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }

    private async Task<Guid> CreateResource(string name)
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/resources", new CreateResourceRequest(name, "SERVICE"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ResourceResponse>())!.Id;
    }

    private async Task<Guid> CreateContract(CreateContractRequest req)
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<ContractResponse>())!.Id;
    }

    private async Task Activate(Guid contractId)
    {
        SetRequestId();
        var resp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate", content: null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    private async Task DrainOutbox()
    {
        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }
    }

    // Lê os commitments persistidos com Id + purpose (o DTO de ativação não expõe purpose/valor).
    private Task<List<Row>> Commitments(Guid contractId) => ExecuteDbContextAsync(async db =>
    {
        var c = await db.EconomicContracts.AsNoTracking().FirstAsync(x => x.Id.Equals(EconomicContractId.From(contractId)));
        return c.Commitments
            .Select(x => new Row(x.Id.Value, x.Direction.Name, x.Purpose.Name, x.Period.Year, x.Period.Month, x.ExpectedAmount.Amount, x.Status.Name))
            .ToList();
    });

    private Task<int> FulfilledCount(Guid contractId) => ExecuteDbContextAsync(db =>
        db.EconomicContracts.AsNoTracking()
            .Where(c => c.Id.Equals(EconomicContractId.From(contractId)))
            .SelectMany(c => c.Commitments)
            .CountAsync(c => c.Status == CommitmentStatus.Fulfilled));

    private async Task Consume(Guid contractId, Guid inflowId, DateTime at)
    {
        SetRequestId();
        var r = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption", new RegisterConsumptionRequest(contractId, inflowId, at));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    private async Task Pay(Guid contractId, Guid outflowId, decimal amount, DateTime at)
    {
        SetRequestId();
        var r = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment", new RegisterPaymentRequest(contractId, outflowId, amount, "BRL", at));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    // Consome e paga a trilha RENT dos meses [fromMonth, toMonth) no dia 8 de cada competência (dentro da janela, sem penalidade).
    private async Task ConsumeAndPayMonths(Guid contractId, DateOnly start, int fromMonth, int toMonth, decimal amount)
    {
        for (var n = fromMonth; n < toMonth; n++)
        {
            var d = start.AddMonths(n);
            var rows = await Commitments(contractId);
            var at = new DateTime(d.Year, d.Month, 8, 12, 0, 0, DateTimeKind.Utc);
            await Consume(contractId, rows.Single(r => r.Direction == "INFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month).Id, at);
            await Pay(contractId, rows.Single(r => r.Direction == "OUTFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month).Id, amount, at);
        }
    }

    private async Task Adjust(Guid contractId, DateOnly effectiveFrom, decimal indexRate)
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/adjust",
            new AdjustContractRequest("RENT", effectiveFrom.Year, effectiveFrom.Month, null, indexRate, "BRL"));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // S1 — Aluguel curto (3 meses), só aluguel, pago em dia: 6 commitments cumpridos, sem penalidade, término normal.
    [Fact]
    public async Task S1_ShortRentOnly_PaidOnTime_ShouldFulfillAllAndTerminate()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 2000m, "BRL", "ACQUISITION", "MONTHLY", 5, 3, start));
        await Activate(contractId);

        for (var n = 0; n < 3; n++)
        {
            var d = start.AddMonths(n);
            var rows = await Commitments(contractId);
            var inflow = rows.Single(r => r.Direction == "INFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month).Id;
            var outflow = rows.Single(r => r.Direction == "OUTFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month).Id;
            var at = new DateTime(d.Year, d.Month, 8, 12, 0, 0, DateTimeKind.Utc);
            await Consume(contractId, inflow, at);
            await Pay(contractId, outflow, 2000m, at);
        }

        await DrainOutbox();
        Assert.Equal(6, await FulfilledCount(contractId));

        SetRequestId();
        var term = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("term_ended", start.AddMonths(3)));
        Assert.Equal(HttpStatusCode.OK, term.StatusCode);
        Assert.DoesNotContain(await Commitments(contractId), c => c.Purpose == "PENALTY");
    }

    // S3 — 12 meses com condomínio: cada mês um boleto único (bundled) paga aluguel + condomínio; 48 commitments cumpridos.
    [Fact]
    public async Task S3_RentWithCondominium_BundledMonthlyPayment_ShouldFulfillBothTracks()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var condoResourceId = await CreateResource("Condomínio");
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);

        var contractId = await CreateContract(new CreateContractRequest(
            agentId, resourceId, 5000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start,
            Charges: [new ContractChargeRequest("CONDOMINIUM", 700m, "BRL", condoResourceId, agentId, true)]));
        await Activate(contractId);
        Assert.Equal(48, (await Commitments(contractId)).Count);

        for (var n = 0; n < 12; n++)
        {
            var d = start.AddMonths(n);
            var at = new DateTime(d.Year, d.Month, 8, 12, 0, 0, DateTimeKind.Utc);
            var rows = await Commitments(contractId);

            foreach (var inflow in rows.Where(r => r.Direction == "INFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month))
                await Consume(contractId, inflow.Id, at);

            var rentOut = rows.Single(r => r.Direction == "OUTFLOW_PROMISE" && r.Purpose == "RENT" && r.Year == d.Year && r.Month == d.Month).Id;
            var condoOut = rows.Single(r => r.Direction == "OUTFLOW_PROMISE" && r.Purpose == "CONDOMINIUM" && r.Year == d.Year && r.Month == d.Month).Id;
            SetRequestId();
            var bundled = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/bundled",
                new RegisterBundledPaymentRequest(contractId,
                    [new BundledPaymentAllocationRequest(rentOut, 5000m), new BundledPaymentAllocationRequest(condoOut, 700m)], "BRL", at));
            Assert.Equal(HttpStatusCode.Created, bundled.StatusCode);
        }

        await DrainOutbox();
        Assert.Equal(48, await FulfilledCount(contractId));
    }

    // S5 — Reajuste anual: 24 meses, +10% a partir do mês 13; meses anteriores ficam em 1000, futuros em 1100.
    [Fact]
    public async Task S5_AnnualAdjustment_ShouldRepriceFutureOnly()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        // Termo datado a partir de hoje: as 24 parcelas ficam todas em aberto (reajuste não toca período cumprido).
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 24, start));
        await Activate(contractId);

        var m13 = start.AddMonths(12);
        SetRequestId();
        var adjust = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/adjust",
            new AdjustContractRequest("RENT", m13.Year, m13.Month, null, 0.10m, "BRL"));
        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);

        var rows = await Commitments(contractId);
        bool OnOrAfter(Row r) => r.Year > m13.Year || (r.Year == m13.Year && r.Month >= m13.Month);
        Assert.All(rows.Where(r => r.Direction == "OUTFLOW_PROMISE" && !OnOrAfter(r)), r => Assert.Equal(1000m, r.Amount));
        Assert.All(rows.Where(r => r.Direction == "OUTFLOW_PROMISE" && OnOrAfter(r)), r => Assert.Equal(1100m, r.Amount));
    }

    // S5b — Reajuste com histórico cumprido (12 meses, +10% no mês 7): meses 1–6 pagos/consumidos a 1000 ficam
    // travados; os meses 7–12 são reprecificados para 1100 e pagos/consumidos no valor novo; 24 cumpridos no total.
    // (O contrato não pode iniciar há mais de 1 ano — MAX_START_DATE_PAST_YEARS — então a jornada cabe em 12 meses.)
    [Fact]
    public async Task S5b_AnnualAdjustment_FulfilledBeforeAndAfter()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start));
        await Activate(contractId);

        await ConsumeAndPayMonths(contractId, start, 0, 6, 1000m);
        await DrainOutbox();
        Assert.Equal(12, await FulfilledCount(contractId));

        var m7 = start.AddMonths(6);
        await Adjust(contractId, m7, 0.10m);

        var rows = await Commitments(contractId);
        bool OnOrAfter(Row r) => r.Year > m7.Year || (r.Year == m7.Year && r.Month >= m7.Month);
        Assert.All(rows.Where(r => r.Direction == "OUTFLOW_PROMISE" && !OnOrAfter(r)), r =>
        {
            Assert.Equal(1000m, r.Amount);
            Assert.Equal("FULFILLED", r.Status);
        });
        Assert.All(rows.Where(r => r.Direction == "OUTFLOW_PROMISE" && OnOrAfter(r)), r =>
        {
            Assert.Equal(1100m, r.Amount);
            Assert.Equal("PROMISED", r.Status);
        });

        await ConsumeAndPayMonths(contractId, start, 6, 12, 1100m);
        await DrainOutbox();
        Assert.Equal(24, await FulfilledCount(contractId));
        Assert.DoesNotContain(await Commitments(contractId), c => c.Purpose == "PENALTY");
    }

    // S6 — Reajuste semestral: dois reajustes de 5% (meses 7 e 13) compõem; última parcela = 1000 × 1.05 × 1.05.
    [Fact]
    public async Task S6_SemestralAdjustments_ShouldCompound()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 18, start));
        await Activate(contractId);

        foreach (var monthsAhead in new[] { 6, 12 })
        {
            var eff = start.AddMonths(monthsAhead);
            SetRequestId();
            var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/adjust",
                new AdjustContractRequest("RENT", eff.Year, eff.Month, null, 0.05m, "BRL"));
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        var last = start.AddMonths(17);
        var lastOutflow = (await Commitments(contractId)).Single(r => r.Direction == "OUTFLOW_PROMISE" && r.Year == last.Year && r.Month == last.Month);
        Assert.Equal(1102.50m, lastOutflow.Amount);
    }

    // S6b — Reajustes compostos com cumprimento entre eles (12 meses, 5% nos meses 5 e 9): meses 1–4 pagos a 1000,
    // meses 5–8 a 1050, meses 9–12 a 1102.50 (1000 × 1.05 × 1.05); 24 commitments cumpridos por banda de valor.
    [Fact]
    public async Task S6b_SemestralAdjustments_FulfilledBetweenAdjustments()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start));
        await Activate(contractId);

        await ConsumeAndPayMonths(contractId, start, 0, 4, 1000m);
        await DrainOutbox();
        var m5 = start.AddMonths(4);
        await Adjust(contractId, m5, 0.05m);

        bool OnOrAfter(Row r, DateOnly from) => r.Year > from.Year || (r.Year == from.Year && r.Month >= from.Month);
        Assert.All((await Commitments(contractId)).Where(r => r.Direction == "OUTFLOW_PROMISE" && OnOrAfter(r, m5)),
            r => Assert.Equal(1050m, r.Amount));

        await ConsumeAndPayMonths(contractId, start, 4, 8, 1050m);
        await DrainOutbox();
        var m9 = start.AddMonths(8);
        await Adjust(contractId, m9, 0.05m);

        Assert.All((await Commitments(contractId)).Where(r => r.Direction == "OUTFLOW_PROMISE" && OnOrAfter(r, m9)),
            r => Assert.Equal(1102.50m, r.Amount));

        await ConsumeAndPayMonths(contractId, start, 8, 12, 1102.50m);
        await DrainOutbox();

        Assert.Equal(24, await FulfilledCount(contractId));
        var outflows = (await Commitments(contractId)).Where(r => r.Direction == "OUTFLOW_PROMISE").ToList();
        Assert.All(outflows, r => Assert.Equal("FULFILLED", r.Status));
        Assert.All(outflows.Where(r => !OnOrAfter(r, m5)), r => Assert.Equal(1000m, r.Amount));
        Assert.All(outflows.Where(r => OnOrAfter(r, m5) && !OnOrAfter(r, m9)), r => Assert.Equal(1050m, r.Amount));
        Assert.All(outflows.Where(r => OnOrAfter(r, m9)), r => Assert.Equal(1102.50m, r.Amount));
    }

    // S7 — Rescisão antecipada no mês 5 de 12: commitments de período posterior são cancelados.
    [Fact]
    public async Task S7_EarlyTermination_ShouldCancelFutureCommitments()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-5);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 3000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start));
        await Activate(contractId);

        SetRequestId();
        var term = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("early_termination", start.AddMonths(5)));
        Assert.Equal(HttpStatusCode.OK, term.StatusCode);

        Assert.Contains(await Commitments(contractId), c => c.Status == "CANCELLED");
    }

    // S7b — Rescisão antecipada com histórico cumprido: meses 1–5 pagos/consumidos ficam FULFILLED intactos;
    // a rescisão no fim do mês 5 cancela todos os 14 commitments restantes (nada permanece em aberto).
    [Fact]
    public async Task S7b_EarlyTermination_AfterFulfilledMonths()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-5);

        var contractId = await CreateContract(new CreateContractRequest(agentId, resourceId, 3000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start));
        await Activate(contractId);

        await ConsumeAndPayMonths(contractId, start, 0, 5, 3000m);
        await DrainOutbox();
        Assert.Equal(10, await FulfilledCount(contractId));

        // Rescisão no último dia do 5º mês: coincide com o LastDay do último período ocupado (CTR20 passa)
        // e antecede o FirstDay de todos os meses seguintes (cascata cancela tudo que sobrou).
        var m5 = start.AddMonths(4);
        var terminationDate = new DateOnly(m5.Year, m5.Month, DateTime.DaysInMonth(m5.Year, m5.Month));
        SetRequestId();
        var term = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("early_termination", terminationDate));
        Assert.Equal(HttpStatusCode.OK, term.StatusCode);

        var rows = await Commitments(contractId);
        Assert.Equal(10, rows.Count(r => r.Status == "FULFILLED"));
        Assert.Equal(14, rows.Count(r => r.Status == "CANCELLED"));
        Assert.DoesNotContain(rows, r => r.Status == "PROMISED");
    }

    // S8 — Seguro como contrato separado (PrimaryPurpose=INSURANCE): trilha marcada INSURANCE; prêmio cumprido.
    [Fact]
    public async Task S8_InsuranceAsSeparateContract_ShouldFulfillPremium()
    {
        SetRequestId();
        var (_, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var coverageResourceId = await CreateResource("Cobertura de seguro");
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        var contractId = await CreateContract(new CreateContractRequest(
            agentId, coverageResourceId, 250m, "BRL", "ACQUISITION", "MONTHLY", 10, 1, start, PrimaryPurpose: "INSURANCE"));
        await Activate(contractId);

        var rows = await Commitments(contractId);
        Assert.All(rows, c => Assert.Equal("INSURANCE", c.Purpose));

        var at = new DateTime(start.Year, start.Month, 12, 12, 0, 0, DateTimeKind.Utc);
        await Consume(contractId, rows.Single(r => r.Direction == "INFLOW_PROMISE").Id, at);
        await Pay(contractId, rows.Single(r => r.Direction == "OUTFLOW_PROMISE").Id, 250m, at);

        await DrainOutbox();
        Assert.Equal(2, await FulfilledCount(contractId));
    }

    // S9 — Condomínio + seguro incêndio com reajuste no mês 7 (12 meses): cada mês um boleto bundled paga as 3
    // trilhas; o reajuste de 10% reprecifica só RENT (charges intactas) e os meses restantes fecham no valor novo.
    [Fact]
    public async Task S9_AnnualAdjustment_WithCondominiumAndFireInsurance_FullCycle()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var condoResourceId = await CreateResource("Condomínio");
        var insuranceResourceId = await CreateResource("Seguro Incêndio");
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12);

        var contractId = await CreateContract(new CreateContractRequest(
            agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start,
            Charges:
            [
                new ContractChargeRequest("CONDOMINIUM", 300m, "BRL", condoResourceId, agentId, true),
                new ContractChargeRequest("INSURANCE", 150m, "BRL", insuranceResourceId, agentId, true),
            ]));
        await Activate(contractId);
        Assert.Equal(72, (await Commitments(contractId)).Count);

        async Task ConsumeAndPayBundledMonths(int fromMonth, int toMonth, decimal rentAmount)
        {
            for (var n = fromMonth; n < toMonth; n++)
            {
                var d = start.AddMonths(n);
                var at = new DateTime(d.Year, d.Month, 8, 12, 0, 0, DateTimeKind.Utc);
                var rows = await Commitments(contractId);

                foreach (var inflow in rows.Where(r => r.Direction == "INFLOW_PROMISE" && r.Year == d.Year && r.Month == d.Month))
                    await Consume(contractId, inflow.Id, at);

                Guid Outflow(string purpose) => rows.Single(r =>
                    r.Direction == "OUTFLOW_PROMISE" && r.Purpose == purpose && r.Year == d.Year && r.Month == d.Month).Id;
                SetRequestId();
                var bundled = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/bundled",
                    new RegisterBundledPaymentRequest(contractId,
                    [
                        new BundledPaymentAllocationRequest(Outflow("RENT"), rentAmount),
                        new BundledPaymentAllocationRequest(Outflow("CONDOMINIUM"), 300m),
                        new BundledPaymentAllocationRequest(Outflow("INSURANCE"), 150m),
                    ], "BRL", at));
                Assert.Equal(HttpStatusCode.Created, bundled.StatusCode);
            }
        }

        await ConsumeAndPayBundledMonths(0, 6, 1000m);
        await DrainOutbox();
        Assert.Equal(36, await FulfilledCount(contractId));

        var m7 = start.AddMonths(6);
        await Adjust(contractId, m7, 0.10m);

        var afterAdjust = await Commitments(contractId);
        bool OnOrAfter(Row r) => r.Year > m7.Year || (r.Year == m7.Year && r.Month >= m7.Month);
        Assert.All(afterAdjust.Where(r => r.Purpose == "RENT" && r.Direction == "OUTFLOW_PROMISE" && !OnOrAfter(r)), r => Assert.Equal(1000m, r.Amount));
        Assert.All(afterAdjust.Where(r => r.Purpose == "RENT" && r.Direction == "OUTFLOW_PROMISE" && OnOrAfter(r)), r => Assert.Equal(1100m, r.Amount));
        Assert.All(afterAdjust.Where(r => r.Purpose == "CONDOMINIUM"), r => Assert.Equal(300m, r.Amount));
        Assert.All(afterAdjust.Where(r => r.Purpose == "INSURANCE"), r => Assert.Equal(150m, r.Amount));

        await ConsumeAndPayBundledMonths(6, 12, 1100m);
        await DrainOutbox();

        Assert.Equal(72, await FulfilledCount(contractId));
        Assert.DoesNotContain(await Commitments(contractId), c => c.Purpose == "PENALTY");
    }
}
