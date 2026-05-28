namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CreateContractExceptionTests : BaseIntegrationTest
{
    public CreateContractExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // ResourceId que não existe no tenant dispara ECC.RES04 ResourceNotFound → 404.
    [Fact]
    public async Task PostContract_WhenResourceDoesNotExist_ShouldReturnNotFound()
    {
        SetRequestId();
        var agentResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/agents",
            new CreateAgentRequest("Imobiliária X", "OUTSIDE", null, null));
        var agent = await agentResp.Content.ReadFromJsonAsync<AgentResponse>();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            ValidRequest(missingResource: true, agentId: agent!.Id));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.RES04", error!.Id);
    }

    // CounterpartyId (agente) que não existe dispara ECC.AGT04 AgentNotFound → 404.
    [Fact]
    public async Task PostContract_WhenAgentDoesNotExist_ShouldReturnNotFound()
    {
        SetRequestId();
        var resResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Imóvel", "SERVICE"));
        var resource = await resResp.Content.ReadFromJsonAsync<ResourceResponse>();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            ValidRequest(missingAgent: true, resourceId: resource!.Id));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.AGT04", error!.Id);
    }

    // Recurso pertencente a outro tenant é tratado como inexistente para o tenant atual (sem auth, retorna 404 ResourceNotFound).
    [Fact]
    public async Task PostContract_WhenResourceBelongsToOtherTenant_ShouldReturnNotFound()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantB);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(
                agentId, resourceId, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5,
                TermMonths: 12, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1)));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // Já existe contrato Draft/Active no mesmo recurso com termo sobreposto dispara ECC.CTR21 → 409.
    [Fact]
    public async Task PostContract_WhenOverlappingActiveContractExists_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, TermMonths: 12, StartDate: startDate));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, TermMonths: 12, StartDate: startDate.AddMonths(1)));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR21", error!.Id);
    }

    // AnchorDay fora da faixa 1-31 dispara ECC.CTR07 → 400.
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task PostContract_WhenAnchorDayOutOfRange_ShouldReturnBadRequest(int anchorDay)
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", anchorDay,
                TermMonths: 12, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // TermMonths fora da faixa 1-120 dispara ECC.CTR17 → 400.
    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task PostContract_WhenTermMonthsOutOfRange_ShouldReturnBadRequest(int termMonths)
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, termMonths,
                StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR17", error!.Id);
    }

    // StartDate mais de 1 ano no passado dispara ECC.CTR18 → 400.
    [Fact]
    public async Task PostContract_WhenStartDateMoreThanOneYearInPast_ShouldReturnBadRequest()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var tooOld = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-2);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, 12, tooOld));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR18", error!.Id);
    }

    private static CreateContractRequest ValidRequest(
        bool missingResource = false,
        bool missingAgent = false,
        Guid? resourceId = null,
        Guid? agentId = null) => new(
        CounterpartyId: missingAgent ? Guid.NewGuid() : (agentId ?? Guid.NewGuid()),
        ResourceId: missingResource ? Guid.NewGuid() : (resourceId ?? Guid.NewGuid()),
        ExpectedAmount: 8000m,
        Currency: "BRL",
        Direction: "ACQUISITION",
        Periodicity: "MONTHLY",
        AnchorDay: 5,
        TermMonths: 12,
        StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1));

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
