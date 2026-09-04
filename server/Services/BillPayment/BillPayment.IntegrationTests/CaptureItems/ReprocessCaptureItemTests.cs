namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Reabrir um artefato já triado para a cascata de hoje avaliá-lo de novo.
/// </summary>
/// <remarks>
/// O desfecho de um item é do dia em que ele passou: a cascata ganha degraus (a visão entrou na
/// 2.4), o prompt muda e o cadastro muda. Sem isto, reavaliar exigia apagar linha no banco — foi
/// o que travou a primeira medição do extrator de visão.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ReprocessCaptureItemTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    public ReprocessCaptureItemTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri Route => new($"/api/v1/{Tenant.Value}/capture-items", UriKind.Relative);

    // Item em quarentena volta para a fila, e o motivo antigo sai junto — ele era o veredito da
    // cascata anterior, e mantê-lo faria a tela mostrar diagnóstico de uma versão que já mudou.
    [Fact]
    public async Task PostReprocess_WhenItemIsUnrecognized_ShouldSendItBackToTheQueue()
    {
        var itemId = await SeedAsync(item =>
        {
            item.StoreArtifact("sha256:a", "chave", OccurredAt);
            item.MarkUnrecognized("no_instrument_in_document", OccurredAt);
        });

        var response = await Client.PostAsync(new Uri($"{Route}/{itemId.Value}/reprocess", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReprocessResponseDto>();
        Assert.Equal("Unrecognized", body!.PreviousStatus);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Received, stored!.Status);
        Assert.Null(stored.Reason);
    }

    // PDF cifrado também reabre: o cadastro pode ter ganho o documento que deriva a senha depois
    // que o item já tinha sido triado.
    [Fact]
    public async Task PostReprocess_WhenItemIsLocked_ShouldSendItBackToTheQueue()
    {
        var itemId = await SeedAsync(item =>
        {
            item.StoreArtifact("sha256:b", "pending-unlock", OccurredAt);
            item.MarkLocked(OccurredAt);
        });

        var response = await Client.PostAsync(new Uri($"{Route}/{itemId.Value}/reprocess", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Same(CaptureItemStatus.Received, (await LoadAsync(itemId))!.Status);
    }

    // Download falho reabre — a nova tentativa é decisão de quem opera, e é isto que devolve o
    // item à fila sem ninguém mexer no banco.
    [Fact]
    public async Task PostReprocess_WhenDownloadHadFailed_ShouldSendItBackToTheQueue()
    {
        var itemId = await SeedAsync(item => item.MarkLinkFailed("artifact_download_failed", OccurredAt));

        var response = await Client.PostAsync(new Uri($"{Route}/{itemId.Value}/reprocess", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Same(CaptureItemStatus.Received, (await LoadAsync(itemId))!.Status);
    }

    // O que já resolveu NÃO volta para a fila: reprocessar um item Parsed refaria a extração e
    // gastaria o extrator para chegar ao mesmo lugar.
    [Fact]
    public async Task PostReprocess_WhenItemAlreadyParsed_ShouldReturnConflict()
    {
        var itemId = await SeedAsync(item =>
        {
            item.StoreArtifact("sha256:c", "chave", OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
        });

        var response = await Client.PostAsync(new Uri($"{Route}/{itemId.Value}/reprocess", UriKind.Relative), null);

        // Transição inválida é conflito de estado, não erro de entrada: o pedido está bem-formado
        // e é o estado atual do item que o recusa.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Same(CaptureItemStatus.Parsed, (await LoadAsync(itemId))!.Status);
    }

    // Item de outro tenant não existe — isolamento sem exceção.
    [Fact]
    public async Task PostReprocess_FromAnotherTenant_ShouldReturnNotFound()
    {
        var itemId = await SeedAsync(item =>
        {
            item.StoreArtifact("sha256:d", "chave", OccurredAt);
            item.MarkUnrecognized("no_text_layer", OccurredAt);
        });

        var outro = new Guid("0195a1f0-0000-7000-8000-000000000002");
        var response = await Client.PostAsync(
            new Uri($"/api/v1/{outro}/capture-items/{itemId.Value}/reprocess", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task<CaptureItemId> SeedAsync(Action<CaptureItem> arrange)
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                $"caixa-{Guid.CreateVersion7():N}@empresa.com.br",
                CredentialRef.ForLocalVault(Guid.CreateVersion7()),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);

            var item = CaptureItem.Ingest(
                Tenant, source.Id, $"msg-{Guid.CreateVersion7():N}", "anexo.pdf",
                "faturas@fornecedor.com.br", "Boleto", OccurredAt, OccurredAt);

            arrange(item);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

    private sealed record ReprocessResponseDto(Guid Id, string PreviousStatus);
}
