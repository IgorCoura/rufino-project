namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ActivateContractExceptionTests : BaseIntegrationTest
{
    public ActivateContractExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Ativar contrato inexistente dispara ECC.CTR14 ContractNotFound → 404.
    [Fact]
    public async Task Activate_WhenContractDoesNotExist_ShouldReturnNotFound()
    {
        SetRequestId();
        var resp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{Guid.NewGuid()}/activate",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR14", error!.Id);
    }

    // Ativar contrato já Active dispara ECC.CTR16 ContractNotDraft → 409.
    [Fact]
    public async Task Activate_WhenContractAlreadyActive_ShouldReturnConflict()
    {
        var contractId = await CreateAndActivate();

        SetRequestId();
        var resp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR16", error!.Id);
    }

    // Ativar contrato Terminated dispara ECC.CTR16 ContractNotDraft → 409.
    [Fact]
    public async Task Activate_WhenContractAlreadyTerminated_ShouldReturnConflict()
    {
        var contractId = await CreateAndActivate();

        SetRequestId();
        await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("term_ended", DateOnly.FromDateTime(DateTime.UtcNow)));

        SetRequestId();
        var resp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR16", error!.Id);
    }

    private async Task<Guid> CreateAndActivate()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(
                agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5,
                TermMonths: 6, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3),
                Penalty: PenaltyTermsRequest.Default));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate",
            content: null);

        return contract.Id;
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
