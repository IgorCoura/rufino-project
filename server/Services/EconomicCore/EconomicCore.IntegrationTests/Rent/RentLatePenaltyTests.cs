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
public sealed class RentLatePenaltyTests : BaseIntegrationTest
{
    public RentLatePenaltyTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }

    // Pagamento de aluguel após a janela de cumprimento materializa, via relay, uma trilha Penalty (multa + juros).
    [Fact]
    public async Task LatePayment_PaidAfterFulfillmentWindow_ShouldMaterializePenaltyTrack()
    {
        SetRequestId();
        var (resourceId, agentId) = await RentScenarioMother.SeedResourceAndAgentViaApi(Client, KnownIds.TenantA);

        // Início há 2 meses: a janela (anchor 5 + 30 dias) já fechou, e um pagamento tardio não é data futura.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);
        const decimal rent = 8000.00m;

        SetRequestId();
        var createResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/contracts",
            new CreateContractRequest(agentId, resourceId, rent, "BRL", "ACQUISITION", "MONTHLY", 5, 1, startDate));
        var contract = await createResp.Content.ReadFromJsonAsync<ContractResponse>();

        SetRequestId();
        var activateResp = await Client.PostAsync($"/api/v1/{KnownIds.TenantA}/contracts/{contract!.Id}/activate", content: null);
        var activated = await activateResp.Content.ReadFromJsonAsync<ActivateContractResponse>();
        var inflowId = activated!.Commitments.Single(c => c.Direction == "INFLOW_PROMISE").Id;
        var outflowId = activated.Commitments.Single(c => c.Direction == "OUTFLOW_PROMISE").Id;

        var occupiedAt = new DateTime(startDate.Year, startDate.Month, 15, 12, 0, 0, DateTimeKind.Utc);
        // Pagamento 40 dias após o início → depois da janela [dia 5, dia 5 + 30].
        var paidAt = startDate.AddDays(40).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

        SetRequestId();
        await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/consumption",
            new RegisterConsumptionRequest(contract.Id, inflowId, occupiedAt));

        SetRequestId();
        var payResp = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/events/payment",
            new RegisterPaymentRequest(contract.Id, outflowId, rent, "BRL", paidAt));
        Assert.Equal(HttpStatusCode.Created, payResp.StatusCode);

        while (await Processor.ProcessPendingAsync(CancellationToken.None) > 0) { }

        var penalties = await ExecuteDbContextAsync(async db =>
        {
            var contractEntity = await db.EconomicContracts.AsNoTracking()
                .FirstAsync(c => c.Id.Equals(EconomicContractId.From(contract.Id)));
            return contractEntity.Commitments
                .Where(c => c.Purpose == CommitmentPurpose.Penalty)
                .Select(c => (c.Direction.Name, c.ExpectedAmount.Amount))
                .ToList();
        });

        // Par recíproco Penalty (outflow + inflow), com valor positivo (multa + juros).
        Assert.Equal(2, penalties.Count);
        Assert.Contains(penalties, p => p.Name == "OUTFLOW_PROMISE" && p.Amount > 0m);
        Assert.Contains(penalties, p => p.Name == "INFLOW_PROMISE" && p.Amount > 0m);
    }
}
