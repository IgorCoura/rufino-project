namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterPaymentExceptionTests : BaseIntegrationTest
{
    public RegisterPaymentExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Pagar com contrato ainda Draft (não ativado) dispara ECC.CTR05 ContractNotActive → 409.
    [Fact]
    public async Task PostPayment_WhenContractIsStillDraft_ShouldReturnConflict()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5,
                TermMonths: 6, StartDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3)));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract!.Id, Guid.NewGuid(), 8000m, "BRL", DateTime.UtcNow.AddDays(-1)));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR05", error!.Id);
    }

    // Pagar o mesmo commitment duas vezes dispara ECC.CTR44 CommitmentAlreadyCovered → 409 na segunda tentativa.
    [Fact]
    public async Task PostPayment_WhenCommitmentAlreadyFulfilled_ShouldReturnConflict()
    {
        var (contractId, outflowId) = await CreateActivateAndGetFirstOutflow();
        var occurredAt = DateTime.UtcNow.AddDays(-1);

        SetRequestId();
        var first = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, 8000m, "BRL", occurredAt));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        SetRequestId();
        var second = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, 8000m, "BRL", occurredAt));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var error = await second.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR44", error!.Id);
    }

    // CommitmentId que não pertence ao contrato dispara ECC.CTR12 CommitmentNotFound (handler busca dentro do contract da rota).
    [Fact]
    public async Task PostPayment_WhenCommitmentIdDoesNotBelongToContract_ShouldReturnNotFound()
    {
        var (contractId, _) = await CreateActivateAndGetFirstOutflow();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, Guid.NewGuid(), 8000m, "BRL", DateTime.UtcNow.AddDays(-1)));

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR12", error!.Id);
    }

    // Pagar com valor <= 0 dispara ECC.EVT02 InvalidAmount → 400.
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task PostPayment_WhenAmountIsNotPositive_ShouldReturnBadRequest(decimal amount)
    {
        var (contractId, outflowId) = await CreateActivateAndGetFirstOutflow();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, amount, "BRL", DateTime.UtcNow.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // PaidDate no futuro dispara ECC.EVT15 FuturePaidDate → 400.
    [Fact]
    public async Task PostPayment_WhenPaidDateIsInFuture_ShouldReturnBadRequest()
    {
        var (contractId, outflowId) = await CreateActivateAndGetFirstOutflow();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, 8000m, "BRL", DateTime.UtcNow.AddDays(7)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.EVT15", error!.Id);
    }

    // Pagar valor diferente do esperado (pagamento parcial) dispara ECC.CTR19 PaymentAmountMismatch → 400.
    [Fact]
    public async Task PostPayment_WhenAmountDiffersFromExpected_ShouldReturnBadRequest()
    {
        var (contractId, outflowId) = await CreateActivateAndGetFirstOutflow();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, 5000m, "BRL", DateTime.UtcNow.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR19", error!.Id);
    }

    private async Task<(Guid contractId, Guid firstOutflowId)> CreateActivateAndGetFirstOutflow()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 8000m, "BRL",
                "ACQUISITION", "MONTHLY", 5, TermMonths: 6, StartDate: startDate));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync(
            $"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        var firstOutflow = activated!.Commitments
            .Where(c => c.Direction == "OUTFLOW_PROMISE")
            .OrderBy(c => c.PeriodYear).ThenBy(c => c.PeriodMonth)
            .First();

        return (contract.Id, firstOutflow.Id);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
