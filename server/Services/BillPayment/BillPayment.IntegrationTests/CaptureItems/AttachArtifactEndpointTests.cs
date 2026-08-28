namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Net.Http.Headers;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A anexação do boleto <strong>pela borda HTTP</strong> — multipart de verdade.
/// </summary>
/// <remarks>
/// <para>
/// Os testes que já existiam chamavam o comando pelo mediator e por isso não enxergavam nada da
/// ligação HTTP: binding de <c>IFormFile</c>, tipo de mídia da parte, teto de tamanho,
/// autorização. Um defeito ali é invisível para eles e visível para quem usa a tela — que foi
/// exatamente o que aconteceu.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class AttachArtifactEndpointTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly CaptureSourceId Source =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000f1"));

    private static readonly DateTime OccurredAt = new(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc);

    private readonly WebApplicationFactory<Program> _host;

    // O host com a cadeia de captura: é ele que traz o armazenamento em memória. Sem isso o
    // upload bate no substituto que falha por falta de balde — blindagem da suíte, não defeito.
    public AttachArtifactEndpointTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _host = factory.WithCaptureChain();

    // TESTE ÂNCORA: o upload atravessa a borda HTTP e o item volta para a fila.
    [Fact]
    public async Task AttachArtifact_WithAMultipartPdf_ShouldAcceptAndRequeue()
    {
        var itemId = await SeedQuarantinedAsync();

        var response = await PostAsync(itemId, [1, 2, 3, 4], "boleto.pdf", "application/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Received, item!.Status);
        Assert.True(item.ManuallySupplied);
    }

    // Tipo que a cascata não sabe abrir é recusado — e recusado ANTES de gravar no balde.
    [Fact]
    public async Task AttachArtifact_WithAnUnsupportedType_ShouldRefuse()
    {
        var itemId = await SeedQuarantinedAsync();

        var response = await PostAsync(itemId, [1, 2, 3], "planilha.xlsx", "application/vnd.ms-excel");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Unrecognized, item!.Status);
        Assert.False(item.ManuallySupplied);
    }

    // Arquivo vazio não é anexo: o item continua esperando documento de verdade.
    [Fact]
    public async Task AttachArtifact_WithAnEmptyFile_ShouldRefuse()
    {
        var itemId = await SeedQuarantinedAsync();

        var response = await PostAsync(itemId, [], "vazio.pdf", "application/pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Item de outro tenant não existe para este — 404, nunca 403, para não confirmar existência.
    [Fact]
    public async Task AttachArtifact_OnAnotherTenantsItem_ShouldNotBeFound()
    {
        var itemId = await SeedQuarantinedAsync();

        var response = await PostAsync(
            itemId, [1, 2, 3, 4], "boleto.pdf", "application/pdf", tenantId: TestTenants.Secondary);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(
        CaptureItemId id,
        byte[] bytes,
        string fileName,
        string contentType,
        Guid? tenantId = null)
    {
        var tenant = tenantId ?? Tenant.Value;

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // O nome da parte tem de ser "file": é assim que o parâmetro da action se chama, e o
        // binding do IFormFile casa por nome. Errar aqui devolve o arquivo como ausente.
        var form = new MultipartFormDataContent { { file, "file", fileName } };

        var client = _host.CreateClient().Authenticated();
        client.DefaultRequestHeaders.Add("x-requestid", Guid.CreateVersion7().ToString());

        return client.PostAsync(
            new Uri($"/api/v1/{tenant}/capture-items/{id.Value}/artifact", UriKind.Relative),
            form);
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId id)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id));

    private Task<CaptureItemId> SeedQuarantinedAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, Source, "AAMkAGI2THVSAAA=", "message-body",
                "cobrancas@asaas.com", "Olá, uma cobrança foi gerada para você",
                OccurredAt, OccurredAt, "text/html", fileName: null);

            item.RecordAttemptedLink("https://www.asaas.com/i/55p08vsad5vci3g7", OccurredAt);
            item.StoreArtifact("sha256:abc", CaptureItem.PENDING_REVIEW, OccurredAt);
            item.MarkUnrecognized("no_instrument_in_document", OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
}
