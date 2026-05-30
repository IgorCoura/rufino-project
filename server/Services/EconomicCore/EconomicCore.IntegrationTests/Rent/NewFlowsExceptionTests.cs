namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;
using EconomicCore.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Caminhos de erro dos fluxos novos (encargos, pagamento bundled, reajuste) impostos pela API.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class NewFlowsExceptionTests : BaseIntegrationTest
{
    public NewFlowsExceptionTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }

    // Encargo duplicado (duas trilhas CONDOMINIUM) é rejeitado com 409 ECC.CTR23.
    [Fact]
    public async Task CreateContract_WithDuplicateChargePurpose_ShouldReturn409_CTR23()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 1, start,
                Charges:
                [
                    new ContractChargeRequest("CONDOMINIUM", 200m, "BRL", resourceId, agentId, true),
                    new ContractChargeRequest("CONDOMINIUM", 300m, "BRL", resourceId, agentId, true),
                ]));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR23", error!.Id);
    }

    // Reajuste informando valor absoluto E índice ao mesmo tempo é rejeitado com ECC.CTR42.
    [Fact]
    public async Task Adjust_WithBothAmountAndIndex_ShouldReturnError_CTR42()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        SetRequestId();
        var create = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 12, start));
        var contractId = (await create.Content.ReadFromJsonAsync<ContractResponse>())!.Id;
        SetRequestId();
        await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate", content: null);

        var m6 = start.AddMonths(6);
        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/adjust",
            new AdjustContractRequest("RENT", m6.Year, m6.Month, NewAmount: 1100m, IndexRate: 0.10m, "BRL"));

        Assert.False(resp.IsSuccessStatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR42", error!.Id);
    }

    // Reajuste cuja competência alcança um mês já cumprido (travado) é rejeitado com 409 ECC.CTR40.
    [Fact]
    public async Task Adjust_OverFulfilledPeriod_ShouldReturn409_CTR40()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);

        SetRequestId();
        var create = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 6, start));
        var contractId = (await create.Content.ReadFromJsonAsync<ContractResponse>())!.Id;
        SetRequestId();
        await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate", content: null);

        var firstMonth = start;
        var (inflow, outflow) = await ExecuteDbContextAsync(async db =>
        {
            var c = await db.EconomicContracts.AsNoTracking().FirstAsync(x => x.Id.Equals(EconomicContractId.From(contractId)));
            var inf = c.Commitments.Single(x => x.Direction == CommitmentDirection.InflowPromise && x.Period.Year == firstMonth.Year && x.Period.Month == firstMonth.Month).Id.Value;
            var outf = c.Commitments.Single(x => x.Direction == CommitmentDirection.OutflowPromise && x.Period.Year == firstMonth.Year && x.Period.Month == firstMonth.Month).Id.Value;
            return (inf, outf);
        });

        var at = new DateTime(firstMonth.Year, firstMonth.Month, 8, 12, 0, 0, DateTimeKind.Utc);
        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption", new RegisterConsumptionRequest(contractId, inflow, at));
        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment", new RegisterPaymentRequest(contractId, outflow, 1000m, "BRL", at));
        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/adjust",
            new AdjustContractRequest("RENT", firstMonth.Year, firstMonth.Month, NewAmount: 1100m, IndexRate: null, "BRL"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR40", error!.Id);
    }

    // Pagamento bundled com valor de uma perna divergente do esperado é rejeitado (ECC.CTR19, pagamento exato).
    [Fact]
    public async Task BundledPayment_WithMismatchedAllocationAmount_ShouldReturnError_CTR19()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        SetRequestId();
        var create = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, 1000m, "BRL", "ACQUISITION", "MONTHLY", 5, 1, start));
        var contractId = (await create.Content.ReadFromJsonAsync<ContractResponse>())!.Id;
        SetRequestId();
        await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/activate", content: null);

        var outflow = await ExecuteDbContextAsync(async db =>
        {
            var c = await db.EconomicContracts.AsNoTracking().FirstAsync(x => x.Id.Equals(EconomicContractId.From(contractId)));
            return c.Commitments.Single(x => x.Direction == CommitmentDirection.OutflowPromise).Id.Value;
        });

        var at = new DateTime(start.Year, start.Month, 8, 12, 0, 0, DateTimeKind.Utc);
        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/bundled",
            new RegisterBundledPaymentRequest(contractId, [new BundledPaymentAllocationRequest(outflow, 999m)], "BRL", at));

        Assert.False(resp.IsSuccessStatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR19", error!.Id);
    }
}
