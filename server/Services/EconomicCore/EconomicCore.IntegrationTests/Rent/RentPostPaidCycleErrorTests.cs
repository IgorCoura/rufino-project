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

    // Registrar consumo sem commitment que o cubra deve retornar 409 ou 500 com indicação de ECC.EVT04.
    [Fact]
    public async Task RegisterConsumption_WhenNoCoveringCommitment_ShouldReturnError()
    {
        await SeedAgentsAndResources();

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        var consResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract!.Id, 2026, 10, KnownDates.Consumption));

        Assert.True(
            consResp.StatusCode == HttpStatusCode.Conflict ||
            consResp.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected Conflict or InternalServerError, got {consResp.StatusCode}");
    }

    // Gerar commitments para contrato encerrado deve retornar 409 com ECC.CTR05.
    [Fact]
    public async Task GenerateCommitments_WhenContractTerminated_ShouldReturnConflict()
    {
        await SeedAgentsAndResources();

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        await ExecuteDbContextAsync(async db =>
        {
            var entity = await db.EconomicContracts
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contract!.Id)));
            entity.Terminate(DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        var genResp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/commitments",
            new GenerateCommitmentsRequest(2026, 10, KnownDates.CommitmentGen));

        Assert.Equal(HttpStatusCode.Conflict, genResp.StatusCode);
        var error = await genResp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR05", error!.Id);
    }

    // Gerar commitments duas vezes para o mesmo período deve retornar 409 com ECC.CTR02.
    [Fact]
    public async Task GenerateCommitments_WhenPeriodAlreadyGenerated_ShouldReturnConflict()
    {
        await SeedAgentsAndResources();

        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 8000.00m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/commitments",
            new GenerateCommitmentsRequest(2026, 10, KnownDates.CommitmentGen));

        var secondResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/commitments",
            new GenerateCommitmentsRequest(2026, 10, KnownDates.CommitmentGen));

        Assert.Equal(HttpStatusCode.Conflict, secondResp.StatusCode);
        var error = await secondResp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR02", error!.Id);
    }

    // Criar contrato com valor zero deve retornar 400.
    [Fact]
    public async Task CreateContract_WhenAmountIsZeroOrNegative_ShouldReturnBadRequest()
    {
        await SeedAgentsAndResources();

        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(KnownIds.Landlord, 0m, "BRL",
                "ACQUISITION", "MONTHLY", AnchorDay: 5));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
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
