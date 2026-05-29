namespace EconomicCore.IntegrationTests.Rent;

using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class OutboxWriteTests : BaseIntegrationTest
{
    public OutboxWriteTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Registrar um EconomicResource via API drena o EconomicResourceRegistered para a tabela outbox_messages (não processado).
    [Fact]
    public async Task PostResource_ShouldPersistDomainEventInOutbox()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Apartamento Outbox 1", "SERVICE"));
        resp.EnsureSuccessStatusCode();

        var rows = await ExecuteDbContextAsync(db =>
            db.OutboxMessages.AsNoTracking().ToListAsync());

        Assert.Single(rows);
        Assert.Equal(
            "EconomicCore.Domain.Operational.EconomicResources.Events.EconomicResourceRegistered",
            rows[0].EventType);
        Assert.False(rows[0].Processed);
        Assert.Contains("Apartamento Outbox 1", rows[0].Payload);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
