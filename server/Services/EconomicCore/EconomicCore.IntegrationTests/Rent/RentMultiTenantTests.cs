namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentMultiTenantTests : BaseIntegrationTest
{
    public RentMultiTenantTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Dados semeados para TENANT_B não devem aparecer nas queries feitas pela API (que usa TENANT_A).
    [Fact]
    public async Task RentReports_WhenQueryingAsTenantA_ShouldNotSeeTenantBData()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Add(RentScenarioMother.Company());
            db.Add(RentScenarioMother.Landlord());
            db.Add(RentScenarioMother.Cash());
            db.Add(RentScenarioMother.RentService());
            db.Add(RentScenarioMother.CompanyForTenantB());
            db.Add(RentScenarioMother.LandlordForTenantB());
            db.Add(RentScenarioMother.CashForTenantB());
            db.Add(RentScenarioMother.ServiceForTenantB());
            await db.SaveChangesAsync();
        });

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

        var dreResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/dre?year=2026&month=10");
        Assert.Equal(HttpStatusCode.OK, dreResp.StatusCode);
        var dre = await dreResp.Content.ReadFromJsonAsync<DREResponse>();
        Assert.Equal(8000.00m, dre!.TotalExpense);

        await ExecuteDbContextAsync(async db =>
        {
            var tenantBEvents = await db.EconomicEvents
                .AsNoTracking()
                .Where(e => e.TenantId.Equals(TenantId.From(KnownIds.TenantB)))
                .ToListAsync();
            Assert.Empty(tenantBEvents);
        });
    }
}
