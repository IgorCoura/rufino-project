namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RentPostPaidCycleErrorTests : BaseIntegrationTest
{
    public RentPostPaidCycleErrorTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Gerar commitments para o mesmo período duas vezes deve retornar 409 com ECC.CTR02.
    [Fact]
    public async Task GenerateCommitments_WhenPeriodAlreadyGenerated_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var contractId = await CreateAndActivate(resourceId, agentId);

        var existingPeriod = await ExecuteDbContextAsync(db =>
            db.EconomicContracts.AsNoTracking()
                .Where(c => c.Id.Equals(EconomicContractId.From(contractId)))
                .SelectMany(c => c.Commitments)
                .Select(c => new { c.Period.Year, c.Period.Month })
                .FirstAsync());

        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/commitments",
            new GenerateCommitmentsRequest(existingPeriod.Year, existingPeriod.Month, KnownDates.CommitmentGen));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR02", error!.Id);
    }

    // Gerar commitments em contrato Terminated deve retornar 409 com ECC.CTR05.
    [Fact]
    public async Task GenerateCommitments_WhenContractTerminated_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var contractId = await CreateAndActivate(resourceId, agentId);

        SetRequestId();
        await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/terminate",
            new TerminateContractRequest("term_ended", DateOnly.FromDateTime(DateTime.UtcNow)));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/commitments",
            new GenerateCommitmentsRequest(2030, 6, KnownDates.CommitmentGen));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR05", error!.Id);
    }

    // Criar contrato com valor zero deve retornar 400 (Money exige amount > 0).
    [Fact]
    public async Task CreateContract_WhenAmountIsZeroOrNegative_ShouldReturnBadRequest()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(
                agentId, resourceId, 0m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5,
                TermMonths: 12, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1), Penalty: PenaltyTermsRequest.Default));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<Guid> CreateAndActivate(Guid resourceId, Guid agentId)
    {
        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(
                agentId, resourceId, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5,
                TermMonths: 6, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3), Penalty: PenaltyTermsRequest.Default));
        createResp.EnsureSuccessStatusCode();
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        activateResp.EnsureSuccessStatusCode();

        return contract.Id;
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
