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

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentBundledPaymentTests : BaseIntegrationTest
{
    public RentBundledPaymentTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }

    // Boleto único pagando aluguel + condomínio: um EconomicEvent com 2 alocações fecha as 2 dualidades e cumpre os 4 commitments.
    [Fact]
    public async Task BundledPayment_PayingRentAndCondominium_ShouldCloseBothDualitiesWithOneCashEvent()
    {
        SetRequestId();
        var (rentResourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var condoResourceResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Serviços de condomínio", "SERVICE"));
        Assert.Equal(HttpStatusCode.Created, condoResourceResp.StatusCode);
        var condoResource = await condoResourceResp.Content.ReadFromJsonAsync<ResourceResponse>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-1);
        const decimal rent = 8000.00m;
        const decimal condo = 300.00m;

        SetRequestId();
        var createReq = new CreateContractRequest(
            CounterpartyId: agentId,
            ResourceId: rentResourceId,
            ExpectedAmount: rent,
            Currency: "BRL",
            Direction: "ACQUISITION",
            Periodicity: "MONTHLY",
            AnchorDay: 5,
            TermMonths: 1,
            StartDate: startDate,
            Charges:
            [
                new ContractChargeRequest("CONDOMINIUM", condo, "BRL", condoResource!.Id, agentId, CollectedByCounterparty: true),
            ]);

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        Assert.Equal(HttpStatusCode.OK, activateResp.StatusCode);

        // O DTO de ativação não expõe purpose/amount: resolvo as trilhas lendo os commitments persistidos.
        var commitments = await ExecuteDbContextAsync(async db =>
        {
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contract.Id)));
            return contractEntity.Commitments
                .Select(c => (c.Id.Value, c.Direction.Name, c.Purpose.Name, c.ExpectedAmount.Amount))
                .ToList();
        });

        var rentOutflow = commitments.Single(c => c is { Item2: "OUTFLOW_PROMISE", Item3: "RENT" }).Item1;
        var rentInflow = commitments.Single(c => c is { Item2: "INFLOW_PROMISE", Item3: "RENT" }).Item1;
        var condoOutflow = commitments.Single(c => c is { Item2: "OUTFLOW_PROMISE", Item3: "CONDOMINIUM" }).Item1;
        var condoInflow = commitments.Single(c => c is { Item2: "INFLOW_PROMISE", Item3: "CONDOMINIUM" }).Item1;

        var occurredAt = new DateTime(startDate.Year, startDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);

        // Consumo de ambas as trilhas (aluguel e condomínio).
        foreach (var inflowId in new[] { rentInflow, condoInflow })
        {
            SetRequestId();
            var occResp = await Client.PostAsJsonAsync(
                $"/api/v1/{KnownIds.TenantA}/events/consumption",
                new RegisterConsumptionRequest(contract.Id, inflowId, occurredAt));
            Assert.Equal(HttpStatusCode.Created, occResp.StatusCode);
        }

        // Um único pagamento (boleto) cobrindo as duas trilhas de outflow.
        SetRequestId();
        var bundledResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/events/payment/bundled",
            new RegisterBundledPaymentRequest(
                contract.Id,
                [
                    new BundledPaymentAllocationRequest(rentOutflow, rent),
                    new BundledPaymentAllocationRequest(condoOutflow, condo),
                ],
                "BRL",
                occurredAt));
        Assert.Equal(HttpStatusCode.Created, bundledResp.StatusCode);
        var bundled = await bundledResp.Content.ReadFromJsonAsync<BundledPaymentEventResponse>();
        Assert.Equal(8300.00m, bundled!.TotalAmount);
        Assert.Equal(2, bundled.AllocationCount);

        // Drena o relay: aqui as duas dualidades fecham e os 4 commitments são cumpridos.
        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        var fulfilledCount = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contract.Id)))
                .SelectMany(c => c.Commitments)
                .CountAsync(c => c.Status == CommitmentStatus.Fulfilled));
        Assert.Equal(4, fulfilledCount);

        // Existe exatamente um evento de caixa (outflow) com 2 alocações e 2 duality links.
        var (outflowEvents, allocations, links) = await ExecuteDbContextAsync(async db =>
        {
            var evs = await db.EconomicEvents.AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)) && e.Direction == Domain.Operational.EconomicEvents.Enumerations.FlowDirection.Outflow)
                .ToListAsync();
            return (evs.Count, evs.Sum(e => e.Allocations.Count), evs.Sum(e => e.DualityLinks.Count));
        });
        Assert.Equal(1, outflowEvents);
        Assert.Equal(2, allocations);
        Assert.Equal(2, links);
    }
}
