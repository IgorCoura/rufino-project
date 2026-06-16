namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;
using EconomicCore.Infra.Outbox;
using Microsoft.Extensions.DependencyInjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentPostPaidCycleTests : BaseIntegrationTest
{
    public RentPostPaidCycleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    // Jornada completa de um contrato de 12 meses: cria resource e agent via API, contrato Draft, ativa (24 commitments), executa 12 ciclos de occupancy+payment, encerra por término normal. Estado final: contract TERMINATED, 24 commitments FULFILLED, 24 events com Duality.
    [Fact]
    public async Task FullLifecycle_WhenEveryMonthIsOccupiedAndPaid_ShouldEndConsistent()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-12);
        const int termMonths = 12;
        const decimal monthlyAmount = 8000.00m;

        SetRequestId();
        var createReq = new CreateContractRequest(
            CounterpartyId: agentId,
            ResourceId: resourceId,
            ExpectedAmount: monthlyAmount,
            Currency: "BRL",
            Direction: "ACQUISITION",
            Periodicity: "MONTHLY",
            AnchorDay: 5,
            TermMonths: termMonths,
            StartDate: startDate,
            Penalty: PenaltyTermsRequest.Default);

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();
        Assert.NotNull(contract);
        Assert.Equal("DRAFT", contract!.Status);
        Assert.Equal(resourceId, contract.ResourceId);
        Assert.Equal(termMonths, contract.TermMonths);

        var commitmentsAtDraft = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contract.Id)))
                .SelectMany(c => c.Commitments)
                .CountAsync());
        Assert.Equal(0, commitmentsAtDraft);

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/activate", content: null);
        Assert.Equal(HttpStatusCode.OK, activateResp.StatusCode);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        Assert.NotNull(activated);
        Assert.Equal("ACTIVE", activated!.Status);
        Assert.Equal(24, activated.Commitments.Count);
        Assert.All(activated.Commitments, c => Assert.Equal("PROMISED", c.Status));

        var outflowsByPeriod = activated.Commitments
            .Where(c => c.Direction == "OUTFLOW_PROMISE")
            .ToDictionary(c => (c.PeriodYear, c.PeriodMonth), c => c.Id);
        var inflowsByPeriod = activated.Commitments
            .Where(c => c.Direction == "INFLOW_PROMISE")
            .ToDictionary(c => (c.PeriodYear, c.PeriodMonth), c => c.Id);
        Assert.Equal(termMonths, outflowsByPeriod.Count);
        Assert.Equal(termMonths, inflowsByPeriod.Count);

        for (var n = 0; n < termMonths; n++)
        {
            var monthDate = startDate.AddMonths(n);
            var key = (monthDate.Year, monthDate.Month);
            var inflowId = inflowsByPeriod[key];
            var outflowId = outflowsByPeriod[key];

            var occurredAt = new DateTime(monthDate.Year, monthDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);

            SetRequestId();
            var occResp = await Client.PostAsJsonAsync(
                $"/api/v1/{KnownIds.TenantA}/events/consumption",
                new RegisterConsumptionRequest(contract.Id, inflowId, occurredAt));
            Assert.Equal(HttpStatusCode.Created, occResp.StatusCode);

            SetRequestId();
            var payResp = await Client.PostAsJsonAsync(
                $"/api/v1/{KnownIds.TenantA}/events/payment",
                new RegisterPaymentRequest(contract.Id, outflowId, monthlyAmount, "BRL", occurredAt));
            Assert.Equal(HttpStatusCode.Created, payResp.StatusCode);
        }

        // Pré-pump: o comando criou os 24 events mas NÃO tocou o contrato — duality e fulfillment são assíncronos (P4).
        var eventsBeforePump = await ExecuteDbContextAsync(db =>
            db.EconomicEvents.AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)))
                .CountAsync());
        Assert.Equal(24, eventsBeforePump);

        var fulfilledBeforePump = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contract.Id)))
                .SelectMany(c => c.Commitments)
                .CountAsync(c => c.Status == CommitmentStatus.Fulfilled));
        Assert.Equal(0, fulfilledBeforePump);

        var dualityBeforePump = await ExecuteDbContextAsync(db =>
            db.EconomicEvents.AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)) && e.DualityLinks.Any())
                .CountAsync());
        Assert.Equal(0, dualityBeforePump);

        // Drena o relay do Outbox até não restar pendência: é aqui que a duality fecha e os commitments são cumpridos.
        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        var fulfilledCount = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contract.Id)))
                .SelectMany(c => c.Commitments)
                .CountAsync(c => c.Status == CommitmentStatus.Fulfilled));
        Assert.Equal(24, fulfilledCount);

        var eventsCount = await ExecuteDbContextAsync(db =>
            db.EconomicEvents.AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)))
                .CountAsync());
        Assert.Equal(24, eventsCount);

        var dualityCount = await ExecuteDbContextAsync(db =>
            db.EconomicEvents.AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)) && e.DualityLinks.Any())
                .CountAsync());
        Assert.Equal(24, dualityCount);

        SetRequestId();
        var terminateResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/terminate",
            new TerminateContractRequest("term_ended", startDate.AddMonths(termMonths)));
        Assert.Equal(HttpStatusCode.OK, terminateResp.StatusCode);
        var terminated = await terminateResp.Content.ReadFromJsonAsync<TerminateContractResponse>();
        Assert.Equal("TERMINATED", terminated!.Status);
        Assert.Equal(0, terminated.CancelledCount);

        var finalStatus = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contract.Id)))
                .Select(c => c.Status.Name)
                .FirstAsync());
        Assert.Equal("TERMINATED", finalStatus);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
