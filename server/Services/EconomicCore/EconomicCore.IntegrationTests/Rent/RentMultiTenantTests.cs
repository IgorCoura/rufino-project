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

    // Eventos persistidos para TenantB não devem aparecer em consultas do TenantA (isolamento explícito por filtro de TenantId).
    [Fact]
    public async Task RentReports_WhenQueryingAsTenantA_ShouldNotSeeTenantBData()
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.Add(RentScenarioMother.CompanyForTenantB());
            db.Add(RentScenarioMother.LandlordForTenantB());
            db.Add(RentScenarioMother.CashForTenantB());
            db.Add(RentScenarioMother.ServiceForTenantB());
            await db.SaveChangesAsync();
        });

        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-2);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(
                agentId, resourceId, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5,
                TermMonths: 2, StartDate: startDate));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();

        var firstPeriod = (startDate.Year, startDate.Month);
        var inflowId = activated!.Commitments
            .First(c => c.Direction == "INFLOW_PROMISE" && c.PeriodYear == firstPeriod.Year && c.PeriodMonth == firstPeriod.Month).Id;
        var outflowId = activated.Commitments
            .First(c => c.Direction == "OUTFLOW_PROMISE" && c.PeriodYear == firstPeriod.Year && c.PeriodMonth == firstPeriod.Month).Id;
        var occurredAt = new DateTime(firstPeriod.Year, firstPeriod.Month, 15, 12, 0, 0, DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, inflowId, occurredAt));

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract.Id, outflowId, 8000.00m, "BRL", occurredAt));

        var dreResp = await Client.GetAsync($"/api/v1/{KnownIds.TenantA}/reports/dre?year={firstPeriod.Year}&month={firstPeriod.Month}");
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

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
