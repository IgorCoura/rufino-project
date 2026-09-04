namespace BillPayment.IntegrationTests.CapturedMessages;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;

/// <summary>
/// O livro-caixa pela porta da frente: lista, filtro, detalhe e estado da sincronização. Só o
/// corpo tinha teste HTTP até 2026-08-28.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CapturedMessagesEndpointTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly TenantId OtherTenant = TenantId.From(TestTenants.Secondary);
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc);

    public CapturedMessagesEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri RouteFor(TenantId tenantId) => new($"/api/v1/{tenantId.Value}/captured-messages", UriKind.Relative);

    // A lista traz os e-mails do tenant, o mais recente primeiro, com o desfecho dominante.
    [Fact]
    public async Task GetList_ShouldReturnTheTenantsMessagesNewestFirst()
    {
        var sourceId = await SeedSourceAsync(Tenant);
        await SeedMessageAsync(Tenant, sourceId, "msg-1", OccurredAt, ArtifactOutcome.Discarded);
        await SeedMessageAsync(Tenant, sourceId, "msg-2", OccurredAt.AddHours(1), ArtifactOutcome.Promoted);

        var page = await Client.GetFromJsonAsync<Page>(RouteFor(Tenant));

        Assert.Equal(2, page!.Items.Count);
        Assert.Equal("Promoted", page.Items[0].Outcome);
        Assert.Equal("Discarded", page.Items[1].Outcome);
    }

    // O filtro por desfecho casa em qualquer anexo.
    [Fact]
    public async Task GetList_FilteredByOutcome_ShouldReturnOnlyTheMatches()
    {
        var sourceId = await SeedSourceAsync(Tenant);
        await SeedMessageAsync(Tenant, sourceId, "msg-1", OccurredAt, ArtifactOutcome.Discarded);
        await SeedMessageAsync(Tenant, sourceId, "msg-2", OccurredAt.AddHours(1), ArtifactOutcome.Promoted);

        var page = await Client.GetFromJsonAsync<Page>(new Uri($"{RouteFor(Tenant)}?outcome=Discarded", UriKind.Relative));

        var only = Assert.Single(page!.Items);
        Assert.Equal("Discarded", only.Outcome);
    }

    // O detalhe traz os anexos com seus desfechos — e diz se a recaptura é possível.
    [Fact]
    public async Task GetById_ShouldReturnTheArtifacts()
    {
        var sourceId = await SeedSourceAsync(Tenant);
        var id = await SeedMessageAsync(Tenant, sourceId, "msg-1", OccurredAt, ArtifactOutcome.Promoted);

        var detail = await Client.GetFromJsonAsync<Dto>(new Uri($"{RouteFor(Tenant)}/{id}", UriKind.Relative));

        Assert.Equal(id, detail!.Id);
        Assert.True(detail.CanRecapture);
        Assert.Equal("Promoted", Assert.Single(detail.Artifacts).Outcome);
    }

    // O cabeçalho da tela: quando a caixa foi lida pela última vez e quantas fontes há.
    [Fact]
    public async Task GetSyncStatus_ShouldReportTheSources()
    {
        await SeedSourceAsync(Tenant);

        var status = await Client.GetFromJsonAsync<SyncStatus>(new Uri($"{RouteFor(Tenant)}/sync-status", UriKind.Relative));

        Assert.Equal(1, status!.SourceCount);
    }

    // Isolamento pela borda: o e-mail de um tenant não aparece na lista nem no detalhe do outro.
    [Fact]
    public async Task AnotherTenant_ShouldNotSeeTheMessage()
    {
        var sourceId = await SeedSourceAsync(Tenant);
        var id = await SeedMessageAsync(Tenant, sourceId, "msg-1", OccurredAt, ArtifactOutcome.Promoted);

        var page = await Client.GetFromJsonAsync<Page>(RouteFor(OtherTenant));
        Assert.Empty(page!.Items);

        var detail = await Client.GetAsync(new Uri($"{RouteFor(OtherTenant)}/{id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    private Task<CaptureSourceId> SeedSourceAsync(TenantId tenantId)
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                tenantId, CaptureSourceKind.MicrosoftGraphMailbox, "Caixa", "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c9")), OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

    private Task<Guid> SeedMessageAsync(
        TenantId tenantId, CaptureSourceId sourceId, string messageId, DateTime receivedAt, ArtifactOutcome outcome)
        => ExecuteDbContextAsync(async db =>
        {
            var message = CapturedMessage.Register(
                tenantId, sourceId, messageId, "faturas@fornecedor.com.br", "Boleto", receivedAt, OccurredAt,
                [("anexo.pdf", "anexo.pdf", "application/pdf")], $"<{messageId}@fornecedor.com.br>");

            message.RecordOutcome(
                "anexo.pdf", outcome, reason: null, captureItemId: null,
                billId: outcome == ArtifactOutcome.Promoted ? BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d9")) : null,
                OccurredAt);

            await db.CapturedMessages.AddAsync(message);
            await db.SaveEntitiesAsync();
            return message.Id.Value;
        });

    private sealed record ArtifactDto(string ArtifactKey, string? FileName, string? ContentType, string Outcome);

    private sealed record Dto(
        Guid Id, Guid SourceId, string Sender, string? Subject, DateTime ReceivedAt, DateTime FirstSeenAt,
        DateTime? ProcessedAt, string Outcome, int ArtifactCount, bool CanRecapture, IReadOnlyList<ArtifactDto> Artifacts);

    private sealed record Page(IReadOnlyList<Dto> Items, string? NextCursor);

    private sealed record SyncStatus(DateTime? LastSyncAt, int SourceCount);
}
