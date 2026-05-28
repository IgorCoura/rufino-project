namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterOccupancyExceptionTests : BaseIntegrationTest
{
    public RegisterOccupancyExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Tentar registrar ocupação para um mês que ainda não iniciou dispara ECC.EVT14 OccupancyInFuture → 409.
    [Fact]
    public async Task PostOccupancy_WhenMonthHasNotStarted_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var futureStart = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, TermMonths: 6, StartDate: futureStart));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();

        var futureInflow = activated!.Commitments
            .Where(c => c.Direction == "INFLOW_PROMISE")
            .OrderBy(c => c.PeriodYear).ThenBy(c => c.PeriodMonth)
            .First();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, futureInflow.Id, DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.EVT14", error!.Id);
    }

    // Registrar ocupação no mesmo commitment duas vezes dispara ECC.CTR03 CannotFulfillInStatus → 409.
    [Fact]
    public async Task PostOccupancy_WhenCommitmentAlreadyFulfilled_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, TermMonths: 3, StartDate: startDate));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();

        var firstInflow = activated!.Commitments
            .Where(c => c.Direction == "INFLOW_PROMISE")
            .OrderBy(c => c.PeriodYear).ThenBy(c => c.PeriodMonth)
            .First();
        var firstOutflow = activated.Commitments
            .Where(c => c.Direction == "OUTFLOW_PROMISE")
            .OrderBy(c => c.PeriodYear).ThenBy(c => c.PeriodMonth)
            .First();
        var occurredAt = new DateTime(firstInflow.PeriodYear, firstInflow.PeriodMonth, 15, 12, 0, 0, DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, firstInflow.Id, occurredAt));
        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract.Id, firstOutflow.Id, 8000m, "BRL", occurredAt));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, firstInflow.Id, occurredAt));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR03", error!.Id);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
