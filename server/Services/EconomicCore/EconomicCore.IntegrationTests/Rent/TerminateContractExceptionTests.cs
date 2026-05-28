namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TerminateContractExceptionTests : BaseIntegrationTest
{
    public TerminateContractExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Terminar contrato já Terminated dispara ECC.CTR13 InvalidContractStatusTransition → 400.
    [Fact]
    public async Task Terminate_WhenContractAlreadyTerminated_ShouldReturnBadRequest()
    {
        var contractId = await CreateAndActivate();

        SetRequestId();
        await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("term_ended", DateOnly.FromDateTime(DateTime.UtcNow)));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("term_ended", DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR13", error!.Id);
    }

    // TerminationDate anterior ao último período Inflow ocupado dispara ECC.CTR20 → 400.
    [Fact]
    public async Task Terminate_WhenDateBeforeLastOccupancy_ShouldReturnBadRequest()
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

        var lastMonth = startDate.AddMonths(2);
        var lastInflow = activated!.Commitments.First(c =>
            c.Direction == "INFLOW_PROMISE"
            && c.PeriodYear == lastMonth.Year && c.PeriodMonth == lastMonth.Month);
        var occurredAt = new DateTime(lastMonth.Year, lastMonth.Month, 15, 12, 0, 0, DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, lastInflow.Id, occurredAt));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/terminate",
            new TerminateContractRequest("term_ended", startDate));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR20", error!.Id);
    }

    private async Task<Guid> CreateAndActivate()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5,
                TermMonths: 3, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1)));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);

        return contract.Id;
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
