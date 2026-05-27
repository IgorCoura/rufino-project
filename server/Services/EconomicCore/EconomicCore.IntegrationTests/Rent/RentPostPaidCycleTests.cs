namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentPostPaidCycleTests : BaseIntegrationTest
{
    public RentPostPaidCycleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Ciclo completo pós-pago: contrato → commitments → consumo out/2026 → pagamento nov/2026 → DRE e cashflow por competência.
    [Fact]
    public async Task RentCycle_WhenConsumptionThenPayment_ShouldCloseDualityAndReportByCompetence()
    {
        await SeedAgentsAndResources();

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();
        Assert.NotNull(contract);
        Assert.Equal("ACTIVE", contract!.Status);

        var genResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/commitments",
            new GenerateCommitmentsRequest(2026, 10, KnownDates.CommitmentGen));
        Assert.Equal(HttpStatusCode.Created, genResp.StatusCode);

        var consResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, 2026, 10, KnownDates.Consumption));
        Assert.Equal(HttpStatusCode.Created, consResp.StatusCode);

        var payResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract.Id, 8000.00m, "BRL", 2026, 10, KnownDates.Payment));
        Assert.Equal(HttpStatusCode.Created, payResp.StatusCode);

        var dreOctResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/dre?year=2026&month=10");
        Assert.Equal(HttpStatusCode.OK, dreOctResp.StatusCode);
        var dreOct = await dreOctResp.Content.ReadFromJsonAsync<DREResponse>();
        Assert.Equal(8000.00m, dreOct!.TotalExpense);

        var dreNovResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/dre?year=2026&month=11");
        var dreNov = await dreNovResp.Content.ReadFromJsonAsync<DREResponse>();
        Assert.Equal(0.00m, dreNov!.TotalExpense);

        var cashNovResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/cashflow?year=2026&month=11");
        Assert.Equal(HttpStatusCode.OK, cashNovResp.StatusCode);
        var cashNov = await cashNovResp.Content.ReadFromJsonAsync<CashFlowResponse>();
        Assert.Equal(8000.00m, cashNov!.TotalOutflow);

        var cashOctResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/cashflow?year=2026&month=10");
        var cashOct = await cashOctResp.Content.ReadFromJsonAsync<CashFlowResponse>();
        Assert.Equal(0.00m, cashOct!.TotalOutflow);
    }

    // Após pagamento, ambos os eventos devem ter dualidade preenchida no banco (scope novo, AsNoTracking).
    [Fact]
    public async Task RentCycle_AfterPayment_ShouldPersistDualityOnBothEvents()
    {
        await SeedAgentsAndResources();

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/commitments",
            new GenerateCommitmentsRequest(2026, 10, KnownDates.CommitmentGen));

        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, 2026, 10, KnownDates.Consumption));

        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract.Id, 8000.00m, "BRL", 2026, 10, KnownDates.Payment));

        var events = await ExecuteDbContextAsync(async db =>
        {
            return await db.EconomicEvents
                .AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantA)))
                .ToListAsync();
        });

        Assert.Equal(2, events.Count);
        Assert.All(events, e =>
        {
            Assert.NotNull(e.Duality);
            Assert.Equal(8000.00m, e.Duality!.MatchedAmount.Amount);
        });
    }

    private async Task SeedAgentsAndResources()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Add(RentScenarioMother.Company());
            db.Add(RentScenarioMother.Landlord());
            db.Add(RentScenarioMother.Cash());
            db.Add(RentScenarioMother.RentService());
            await db.SaveChangesAsync();
        });
    }
}
