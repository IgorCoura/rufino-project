namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;
using EconomicCore.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentLatePaymentOneShotTests : BaseIntegrationTest
{
    private const decimal RENT = 8000.00m;
    private const decimal PENALTY = 160.00m; // 8000 × 2% (multa), 0 meses cheios de atraso → sem juros.

    public RentLatePaymentOneShotTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId(Guid? requestId = null)
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", (requestId ?? Guid.NewGuid()).ToString());
    }

    // Contrato com início no dia 1 de (mês corrente - 2): janela [dia 5, dia 5 + 30] já fechou e o pagamento
    // tardio (start + 40d, ainda no mês seguinte → 0 meses cheios) não cai em data futura nem soma juros.
    private async Task<(Guid ContractId, Guid OutflowId, Guid InflowId, DateOnly StartDate)> SeedActivatedContract()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var today = DateTime.UtcNow;
        var startDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-2);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, RENT, "BRL", "ACQUISITION", "MONTHLY", 5, 1, startDate, PenaltyTermsRequest.Default));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        var inflowId = activated!.Commitments.Single(c => c.Direction == "INFLOW_PROMISE").Id;
        var outflowId = activated.Commitments.Single(c => c.Direction == "OUTFLOW_PROMISE").Id;

        return (contract.Id, outflowId, inflowId, startDate);
    }

    private static DateTime LatePaidAt(DateOnly startDate)
        => startDate.AddDays(40).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

    // Jornada one-shot: boleto único de 8160 (8000 aluguel + 160 multa) gera 1 evento com 2 alocações; após o
    // relay, as 4 pernas (Rent + Penalty) ficam FULFILLED, a perna Rent casa com o consumo e a Penalty é self-settled.
    [Fact]
    public async Task OneShotLatePayment_PaidAfterWindow_ShouldCreateOneEventWithTwoAllocationsAndCloseAllDualities()
    {
        var (contractId, outflowId, inflowId, startDate) = await SeedActivatedContract();
        var occupiedAt = new DateTime(startDate.Year, startDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);

        SetRequestId();
        var consumptionResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contractId, inflowId, occupiedAt));
        var consumption = await consumptionResp.Content.ReadFromJsonAsync<ConsumptionEventResponse>();

        SetRequestId();
        var lateResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contractId, outflowId, RENT + PENALTY, "BRL", LatePaidAt(startDate)));

        Assert.Equal(HttpStatusCode.Created, lateResp.StatusCode);
        var late = await lateResp.Content.ReadFromJsonAsync<LatePaymentEventResponse>();
        Assert.Equal(RENT + PENALTY, late!.TotalAmount);
        Assert.Equal(RENT, late.BaseAmount);
        Assert.Equal(PENALTY, late.PenaltyAmount);
        Assert.Equal(2, late.AllocationCount);

        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        var (allocationCount, dualityLinks) = await ExecuteDbContextAsync(async db =>
        {
            var ev = await db.EconomicEvents.AsNoTracking()
                .FirstAsync(e => e.Id.Equals(EconomicEventId.From(late.Id)));
            return (ev.Allocations.Count, ev.DualityLinks
                .Select(d => (CommitmentId: d.CommitmentId!.Value.Value, CounterpartEventId: d.CounterpartEventId.Value))
                .ToList());
        });

        Assert.Equal(2, allocationCount);
        Assert.Equal(2, dualityLinks.Count);
        // Perna Rent casa com o evento de consumo; perna Penalty é self-settled (counterpart = o próprio evento).
        Assert.Contains(dualityLinks, d => d.CommitmentId == outflowId && d.CounterpartEventId == consumption!.Id);
        Assert.Contains(dualityLinks, d => d.CommitmentId == late.PenaltyCommitmentId && d.CounterpartEventId == late.Id);

        var commitments = await ExecuteDbContextAsync(async db =>
        {
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            return contractEntity.Commitments
                .Select(c => (Purpose: c.Purpose, StatusName: c.Status.Name, Amount: c.ExpectedAmount.Amount))
                .ToList();
        });

        Assert.Equal(4, commitments.Count);
        Assert.All(commitments, c => Assert.Equal("FULFILLED", c.StatusName));
        Assert.Equal(2, commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty && c.Amount == PENALTY));
    }

    // Total informado diferente de base + penalidade retorna 400 ECC.CTR45 e não materializa a trilha Penalty.
    [Fact]
    public async Task OneShotLatePayment_WithWrongTotal_ShouldReturn400_CTR45()
    {
        var (contractId, outflowId, _, startDate) = await SeedActivatedContract();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contractId, outflowId, RENT + 999m, "BRL", LatePaidAt(startDate)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR45", error!.Id);

        var penaltyCount = await ExecuteDbContextAsync(async db =>
        {
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            return contractEntity.Commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty);
        });
        Assert.Equal(0, penaltyCount);
    }

    // Pagamento dentro da janela de cumprimento retorna 400 ECC.CTR46 (deve usar o fluxo de pagamento normal).
    [Fact]
    public async Task OneShotLatePayment_PaidWithinWindow_ShouldReturn400_CTR46()
    {
        var (contractId, outflowId, _, startDate) = await SeedActivatedContract();
        var withinWindow = startDate.AddDays(20).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contractId, outflowId, RENT + PENALTY, "BRL", withinWindow));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR46", error!.Id);
    }

    // Dois POSTs com o mesmo x-requestid persistem um único evento e uma única trilha Penalty (resposta neutra na 2ª).
    [Fact]
    public async Task OneShotLatePayment_WithSameRequestId_ShouldBeIdempotent()
    {
        var (contractId, outflowId, _, startDate) = await SeedActivatedContract();
        var requestId = Guid.NewGuid();
        var request = new RegisterLatePaymentRequest(contractId, outflowId, RENT + PENALTY, "BRL", LatePaidAt(startDate));

        SetRequestId(requestId);
        var first = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late", request);
        SetRequestId(requestId);
        var second = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var duplicate = await second.Content.ReadFromJsonAsync<LatePaymentEventResponse>();
        Assert.Equal(Guid.Empty, duplicate!.Id);

        var (eventCount, penaltyCount) = await ExecuteDbContextAsync(async db =>
        {
            var events = await db.EconomicEvents.AsNoTracking().CountAsync();
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            return (events, contractEntity.Commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty));
        });
        Assert.Equal(1, eventCount);
        Assert.Equal(2, penaltyCount);
    }

    // Política de valores fixos: multa R$100 única + juros R$10/dia × 6 dias de atraso → boleto único de 8160.
    [Fact]
    public async Task OneShotLatePayment_WithFixedPolicy_ShouldPriceFlatFinePlusPerDayInterest()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var today = DateTime.UtcNow;
        var startDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-2);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, RENT, "BRL", "ACQUISITION", "MONTHLY", 5, 1, startDate,
                new PenaltyTermsRequest("FIXED", 100m, "FIXED", 10m, "DAILY")));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        var outflowId = activated!.Commitments.Single(c => c.Direction == "OUTFLOW_PROMISE").Id;

        // start+40d = 6 dias após o fim da janela (dia 5 + 30d) → penalidade = 100 + 10×6 = 160.
        SetRequestId();
        var lateResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contract.Id, outflowId, RENT + 160m, "BRL", LatePaidAt(startDate)));

        Assert.Equal(HttpStatusCode.Created, lateResp.StatusCode);
        var late = await lateResp.Content.ReadFromJsonAsync<LatePaymentEventResponse>();
        Assert.Equal(RENT, late!.BaseAmount);
        Assert.Equal(160m, late.PenaltyAmount);
        Assert.Equal(2, late.AllocationCount);
    }

    // O relay rechama TryRegisterLatePenalty para a perna Rent do bundled: no-op por período — segue exatamente 1 par Penalty.
    [Fact]
    public async Task OneShotLatePayment_AfterRelayProcessing_ShouldNotDuplicatePenaltyTrack()
    {
        var (contractId, outflowId, inflowId, startDate) = await SeedActivatedContract();
        var occupiedAt = new DateTime(startDate.Year, startDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contractId, inflowId, occupiedAt));

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contractId, outflowId, RENT + PENALTY, "BRL", LatePaidAt(startDate)));

        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        var penaltyCount = await ExecuteDbContextAsync(async db =>
        {
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            return contractEntity.Commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty);
        });
        Assert.Equal(2, penaltyCount);
    }
}
