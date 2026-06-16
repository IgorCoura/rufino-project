namespace EconomicCore.IntegrationTests.Rent;

using System.Net;
using System.Net.Http.Json;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using EconomicCore.IntegrationTests.Mothers;
using EconomicCore.Infra.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ChangeContractPenaltyTermsTests : BaseIntegrationTest
{
    private const decimal RENT = 8000.00m;

    public ChangeContractPenaltyTermsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId(Guid? requestId = null)
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", (requestId ?? Guid.NewGuid()).ToString());
    }

    // Contrato com início no dia 1 de (mês corrente - 2): janela [dia 5, dia 5 + 30] já fechou; start + 40d
    // está 6 dias após a janela (e ainda no mês seguinte → 0 meses cheios na política mensal).
    private async Task<(Guid ContractId, Guid OutflowId, Guid InflowId, DateOnly StartDate)> SeedActivatedContract()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        var today = DateTime.UtcNow;
        var startDate = new DateOnly(today.Year, today.Month, 1).AddMonths(-2);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, RENT, "BRL", "ACQUISITION", "MONTHLY", 5, 1, startDate, PenaltyTermsRequest.Default));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        var inflowId = activated!.Commitments.Single(c => c.Direction == "INFLOW_PROMISE").Id;
        var outflowId = activated.Commitments.Single(c => c.Direction == "OUTFLOW_PROMISE").Id;

        return (contract.Id, outflowId, inflowId, startDate);
    }

    // Alterar a política em contrato Draft retorna 200 com a nova política persistida.
    [Fact]
    public async Task PostPenalty_OnDraftContract_ShouldReplacePolicy()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, RENT, "BRL", "ACQUISITION", "MONTHLY", 5, 12, startDate, PenaltyTermsRequest.Default));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/penalty",
            new ChangePenaltyTermsRequest("FIXED", 50m, "FIXED", 10m, "DAILY"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var changed = await resp.Content.ReadFromJsonAsync<ChangePenaltyTermsResponse>();
        Assert.Equal(contract.Id, changed!.Id);
        Assert.Equal("FIXED", changed.FineKind);
        Assert.Equal(50m, changed.FineValue);
        Assert.Equal("FIXED", changed.InterestKind);
        Assert.Equal(10m, changed.InterestValue);
        Assert.Equal("DAILY", changed.InterestPeriod);

        var persisted = await ExecuteDbContextAsync(async db =>
        {
            var entity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contract.Id)));
            return (entity.PenaltyPolicy.FineKind.Name, entity.PenaltyPolicy.FineValue, entity.PenaltyPolicy.InterestPeriod.Name);
        });
        Assert.Equal(("FIXED", 50m, "DAILY"), persisted);
    }

    // Alteração em contrato Active vale para o próximo atraso: multa renegociada para 5% → one-shot exige total 8000 + 400.
    [Fact]
    public async Task PostPenalty_OnActiveContract_ShouldApplyToSubsequentLatePayment()
    {
        var (contractId, outflowId, _, startDate) = await SeedActivatedContract();

        SetRequestId();
        var changeResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/penalty",
            new ChangePenaltyTermsRequest("PERCENT", 0.05m, "PERCENT", 0m, "MONTHLY"));
        Assert.Equal(HttpStatusCode.OK, changeResp.StatusCode);

        SetRequestId();
        var lateResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment/late",
            new RegisterLatePaymentRequest(contractId, outflowId, RENT + 400m, "BRL",
                startDate.AddDays(40).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.Created, lateResp.StatusCode);
        var late = await lateResp.Content.ReadFromJsonAsync<LatePaymentEventResponse>();
        Assert.Equal(400m, late!.PenaltyAmount);
    }

    // Penalidade materializada antes da alteração mantém o valor antigo: a penalty de 160 (2%) segue pagável
    // pelo valor original mesmo após a multa subir para 5% — a obrigação materializada prevalece.
    [Fact]
    public async Task PostPenalty_AfterPenaltyMaterialized_ShouldNotRepriceExistingPenalty()
    {
        var (contractId, outflowId, inflowId, startDate) = await SeedActivatedContract();
        var occupiedAt = new DateTime(startDate.Year, startDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);
        var paidAt = startDate.AddDays(40).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contractId, inflowId, occupiedAt));
        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, outflowId, RENT, "BRL", paidAt));
        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        SetRequestId();
        var changeResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/penalty",
            new ChangePenaltyTermsRequest("PERCENT", 0.05m, "PERCENT", 0.01m, "MONTHLY"));
        Assert.Equal(HttpStatusCode.OK, changeResp.StatusCode);

        var (penaltyOutflowId, penaltyAmount) = await ExecuteDbContextAsync(async db =>
        {
            var entity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            var penaltyOutflow = entity.Commitments.Single(c =>
                c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
            return (penaltyOutflow.Id.Value, penaltyOutflow.ExpectedAmount.Amount);
        });

        // 8000 × 2% (política vigente na materialização), não 8000 × 5%.
        Assert.Equal(160m, penaltyAmount);

        SetRequestId();
        var payPenaltyResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contractId, penaltyOutflowId, 160m, "BRL",
                startDate.AddDays(45).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)));
        Assert.Equal(HttpStatusCode.Created, payPenaltyResp.StatusCode);
    }

    // Alterar a política de um contrato Terminated retorna 409 ECC.CTR51.
    [Fact]
    public async Task PostPenalty_OnTerminatedContract_ShouldReturn409_CTR51()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, RENT, "BRL", "ACQUISITION", "MONTHLY", 5, 12, startDate, PenaltyTermsRequest.Default));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/terminate",
            new TerminateContractRequest("Desistência antes da ativação", startDate));

        SetRequestId();
        var resp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract.Id}/penalty",
            new ChangePenaltyTermsRequest("PERCENT", 0.05m, "PERCENT", 0.01m, "MONTHLY"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("ECC.CTR51", error!.Id);
    }

    // Dois POSTs com o mesmo x-requestid: o segundo devolve a resposta neutra (Id vazio) sem repetir o efeito.
    [Fact]
    public async Task PostPenalty_WithSameRequestId_ShouldBeIdempotent()
    {
        var (contractId, _, _, _) = await SeedActivatedContract();
        var requestId = Guid.NewGuid();
        var request = new ChangePenaltyTermsRequest("FIXED", 75m, "PERCENT", 0.02m, "MONTHLY");

        SetRequestId(requestId);
        var first = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/penalty", request);
        SetRequestId(requestId);
        var second = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contractId}/penalty", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var duplicate = await second.Content.ReadFromJsonAsync<ChangePenaltyTermsResponse>();
        Assert.Equal(Guid.Empty, duplicate!.Id);

        var persisted = await ExecuteDbContextAsync(async db =>
        {
            var entity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contractId)));
            return (entity.PenaltyPolicy.FineKind.Name, entity.PenaltyPolicy.FineValue);
        });
        Assert.Equal(("FIXED", 75m), persisted);
    }
}
