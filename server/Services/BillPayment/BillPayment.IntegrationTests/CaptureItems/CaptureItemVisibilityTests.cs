namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;

/// <summary>
/// A quarentena tem dois níveis de visibilidade, e eles são regra de <strong>projeção</strong>,
/// não de UI (ADR-008). Estes testes atravessam o HTTP porque é ali que o vazamento aconteceria.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureItemVisibilityTests : BaseIntegrationTest
{
    private static readonly Guid TenantAGuid = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid TenantBGuid = new("0195a1f0-0000-7000-8000-000000000002");
    private static readonly TenantId TenantA = TenantId.From(TenantAGuid);
    private static readonly TenantId TenantB = TenantId.From(TenantBGuid);
    private static readonly CaptureSourceId Source = CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 10, 9, 30, 0, DateTimeKind.Utc);

    private const string StorageKey = "tenants/a/capture/boleto-enel.pdf";
    private const string SourceUrl = "https://fatura.enel.com.br/2via/abc123";
    private const string ContentHash = "sha256:9f2c4a1b";

    public CaptureItemVisibilityTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/capture-items", UriKind.Relative);

    // Item de OUTRO pagador não devolve nada que leve ao documento — o sistema SABE que não é
    // deste usuário, e o ponteiro do arquivo ou o link da fatura seriam vazamento gratuito.
    [Fact]
    public async Task GetCaptureItem_WhenForeignPayer_ShouldHideEverythingThatLeadsToTheDocument()
    {
        var id = await SeedAsync(item =>
        {
            item.MarkParsed(ExtractionMethod.EmbeddedText, "cnpj_first_5", OccurredAt);
            item.MarkForeign("payer_belongs_to_other_tenant", OccurredAt);
        });

        var dto = await GetItemAsync(TenantAGuid, id);

        Assert.Equal("ForeignPayer", dto.Status);
        Assert.False(dto.HasArtifact);
        Assert.Null(dto.SourceUrl);
        Assert.Null(dto.ContentHash);
        Assert.Null(dto.UnlockedBy);
        Assert.Null(dto.BillId);

        // O que ele PRECISA ver continua vindo: de quem, sobre o quê, quando, e por quê.
        Assert.Equal("faturas@enel.com.br", dto.Sender);
        Assert.Equal("payer_belongs_to_other_tenant", dto.Reason);
        Assert.NotNull(dto.Subject);
    }

    // Item Unrouted devolve o suficiente para o usuário decidir se reivindica — sem isso a fila
    // de reivindicação seria uma lista de assuntos sem contexto.
    [Fact]
    public async Task GetCaptureItem_WhenUnrouted_ShouldExposeEnoughToDecideOnClaiming()
    {
        var id = await SeedAsync(item =>
        {
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            item.MarkUnrouted("no_rule_matched", OccurredAt);
        });

        var dto = await GetItemAsync(TenantAGuid, id);

        Assert.Equal("Unrouted", dto.Status);
        Assert.True(dto.HasArtifact);
        Assert.Equal(SourceUrl, dto.SourceUrl);
        Assert.Equal(ContentHash, dto.ContentHash);
    }

    // Item promovido é do próprio tenant e mostra tudo, inclusive o boleto que gerou.
    [Fact]
    public async Task GetCaptureItem_WhenPromoted_ShouldExposeEverythingIncludingTheBill()
    {
        var billId = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d1"));
        var id = await SeedAsync(item =>
        {
            item.MarkParsed(ExtractionMethod.QrCode, "cnpj_first_5", OccurredAt);
            item.Promote(billId, RoutingConfidence.Strong, OccurredAt);
        });

        var dto = await GetItemAsync(TenantAGuid, id);

        Assert.Equal("Promoted", dto.Status);
        Assert.Equal(billId.Value, dto.BillId);
        Assert.Equal("Strong", dto.RoutingConfidence);
        Assert.Equal("QrCode", dto.ExtractionMethod);
        Assert.Equal("cnpj_first_5", dto.UnlockedBy);
        Assert.True(dto.HasArtifact);
    }

    // Item ainda no funil não expõe, mesmo com artefato e URL já gravados: antes do roteamento
    // ninguém sabe de quem é o documento, e projetar ali vazaria justamente na janela que
    // antecede a descoberta de que o pagador é outro.
    [Fact]
    public async Task GetCaptureItem_WhileStillInPipeline_ShouldNotExposeArtifactPointers()
    {
        var id = await SeedAsync(_ => { });

        var dto = await GetItemAsync(TenantAGuid, id);

        Assert.Equal("LinkPending", dto.Status);
        Assert.False(dto.HasArtifact);
        Assert.Null(dto.SourceUrl);
        Assert.Null(dto.ContentHash);
    }

    // A lista aplica o mesmo filtro que o detalhe — projetar diferente nos dois seria o furo.
    [Fact]
    public async Task ListCaptureItems_ShouldApplyTheSameVisibilityRuleAsTheDetail()
    {
        await SeedAsync(item =>
        {
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            item.MarkForeign("payer_belongs_to_other_tenant", OccurredAt);
        });

        var response = await Client.GetAsync(new Uri($"{RouteFor(TenantAGuid)}?status=ForeignPayer", UriKind.Relative));
        var page = await response.Content.ReadFromJsonAsync<CaptureItemPageDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(page!.Items);
        Assert.False(item.HasArtifact);
        Assert.Null(item.SourceUrl);
    }

    // Filtrar por status monta a fila de quarentena; status desconhecido não derruba a tela.
    [Fact]
    public async Task ListCaptureItems_WithUnknownStatusFilter_ShouldReturnEverythingInsteadOfFailing()
    {
        await SeedAsync(_ => { });

        var response = await Client.GetAsync(new Uri($"{RouteFor(TenantAGuid)}?status=NaoExiste", UriKind.Relative));
        var page = await response.Content.ReadFromJsonAsync<CaptureItemPageDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(page!.Items);
    }

    // O item de um tenant não aparece para o outro — isolamento sem exceção.
    [Fact]
    public async Task ListCaptureItems_ShouldNotSeeAnotherTenantItems()
    {
        await SeedAsync(_ => { }, tenantId: TenantB);

        var response = await Client.GetAsync(RouteFor(TenantAGuid));
        var page = await response.Content.ReadFromJsonAsync<CaptureItemPageDto>();

        Assert.Empty(page!.Items);
    }

    private Task<CaptureItemId> SeedAsync(Action<CaptureItem> arrange, TenantId? tenantId = null)
        => ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                tenantId ?? TenantA,
                Source,
                "AAMkAGI2THVSAAA=",
                "boleto-enel.pdf",
                "faturas@enel.com.br",
                "Sua fatura de energia chegou",
                OccurredAt.AddHours(-1),
                OccurredAt);

            item.MarkLinkPending(SourceUrl, OccurredAt);
            item.StoreArtifact(ContentHash, StorageKey, OccurredAt);
            arrange(item);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

    private async Task<CaptureItemResponseDto> GetItemAsync(Guid tenantId, CaptureItemId id)
    {
        var response = await Client.GetAsync(new Uri($"{RouteFor(tenantId)}/{id.Value}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<CaptureItemResponseDto>())!;
    }
}
