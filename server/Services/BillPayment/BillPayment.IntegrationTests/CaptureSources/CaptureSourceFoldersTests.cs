namespace BillPayment.IntegrationTests.CaptureSources;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Mailboxes;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Acompanhar mais de uma pasta, e reler a caixa por decisão de quem opera.
/// </summary>
/// <remarks>
/// O que estes testes protegem não é a lista de pastas em si — é o fato de <strong>cursor e falha
/// serem por pasta</strong>. Um cursor único obrigaria uma fonte por pasta, duplicando credencial
/// e cadastro para uma caixa só, e uma pasta renomeada no cliente de e-mail derrubaria a captura
/// inteira.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureSourceFoldersTests : BaseIntegrationTest
{
    private static readonly Guid TenantA = new("0195a1f0-0000-7000-8000-000000000001");

    private readonly HttpClient _client;
    private readonly FakeMailboxReader _mailbox;

    public CaptureSourceFoldersTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        var host = factory.WithReachableMailbox();
        _client = host.CreateClient().Authenticated();
        _mailbox = host.Services.GetRequiredService<FakeMailboxReader>();
    }

    private static Uri Route => new($"/api/v1/{TenantA}/capture-sources", UriKind.Relative);

    // Uma varredura passa por TODAS as pastas acompanhadas, cada uma com o próprio cursor.
    [Fact]
    public async Task PostSync_WithSeveralFolders_ShouldSweepEachOneWithItsOwnCursor()
    {
        var sourceId = await ConnectAsync();
        await AddFolderAsync(sourceId, "Contas/Fornecedores");

        _mailbox.ResultsByFolder[FakeMailboxReader.INBOX] =
            MailboxReadResult.Ok([], "delta-inbox", FakeMailboxReader.AttemptedAt);
        _mailbox.ResultsByFolder["Contas/Fornecedores"] =
            MailboxReadResult.Ok([], "delta-contas", FakeMailboxReader.AttemptedAt);

        await SyncAsync(sourceId);

        var stored = await LoadAsync(sourceId);
        Assert.Equal(2, stored.Folders.Count);
        Assert.Equal("delta-inbox", stored.Folders.First(f => f.Path is null).SyncCursor);
        Assert.Equal("delta-contas", stored.Folders.First(f => f.Path == "Contas/Fornecedores").SyncCursor);

        // E a varredura seguinte retoma cada pasta do cursor dela, não de um cursor comum.
        _mailbox.Reads.Clear();
        await SyncAsync(sourceId);

        Assert.Contains((null, "delta-inbox"), _mailbox.Reads);
        Assert.Contains((("Contas/Fornecedores"), "delta-contas"), _mailbox.Reads!);
    }

    // Pasta quebrada registra a própria falha e NÃO impede as outras de sincronizar — é o caso da
    // pasta renomeada no cliente de e-mail, que sem isolamento derrubaria a captura inteira.
    [Fact]
    public async Task PostSync_WhenOneFolderFails_ShouldStillSweepTheOthers()
    {
        var sourceId = await ConnectAsync();
        await AddFolderAsync(sourceId, "Renomeada");

        _mailbox.ResultsByFolder[FakeMailboxReader.INBOX] =
            MailboxReadResult.Ok([], "delta-inbox", FakeMailboxReader.AttemptedAt);
        _mailbox.ResultsByFolder["Renomeada"] =
            MailboxReadResult.Denied("folder_not_found", "404", FakeMailboxReader.AttemptedAt);

        var result = await SyncAsync(sourceId);

        // A falha vence o resumo: quem chamou está conferindo se a conexão funciona.
        Assert.Equal("Denied", result.Status);

        var stored = await LoadAsync(sourceId);
        var inbox = stored.Folders.First(f => f.Path is null);
        var quebrada = stored.Folders.First(f => f.Path == "Renomeada");

        Assert.Equal("delta-inbox", inbox.SyncCursor);
        Assert.Null(inbox.LastSyncError);
        Assert.Equal("folder_not_found", quebrada.LastSyncError);
        Assert.Null(quebrada.SyncCursor);
    }

    // A pasta acrescentada nasce sem cursor: a primeira varredura dela lê tudo o que já está lá,
    // sem reler as pastas que já tinham cursor.
    [Fact]
    public async Task PostFolders_ShouldStartTheNewFolderWithoutCursor()
    {
        var sourceId = await ConnectAsync();
        await SyncAsync(sourceId);

        await AddFolderAsync(sourceId, "Contas");
        _mailbox.Reads.Clear();
        await SyncAsync(sourceId);

        Assert.Contains((null, "deltaLink-1"), _mailbox.Reads);
        Assert.Contains((("Contas"), (string?)null), _mailbox.Reads!);
    }

    // Pasta repetida é recusada com 409 e BLP.CPS16 — normalização decide o que é repetido.
    [Fact]
    public async Task PostFolders_WhenAlreadyMonitored_ShouldReturnConflict()
    {
        var sourceId = await ConnectAsync();
        await AddFolderAsync(sourceId, "Contas");

        var response = await _client.PostAsJsonAsync(
            new Uri($"{Route}/{sourceId}/folders", UriKind.Relative), new { folderPath = "/contas/" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Remover pasta deixa de varrê-la, e as outras seguem intactas.
    [Fact]
    public async Task DeleteFolders_ShouldStopSweepingThatFolderOnly()
    {
        var sourceId = await ConnectAsync();
        await AddFolderAsync(sourceId, "Contas");

        var response = await _client.DeleteAsync(
            new Uri($"{Route}/{sourceId}/folders?folderPath=Contas", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mailbox.Reads.Clear();
        await SyncAsync(sourceId);

        Assert.Single(_mailbox.Reads);
        Assert.Null(Assert.Single(_mailbox.Reads).Folder);
    }

    // Remover a última pasta é recusado: fonte sem pasta não varreria nada e não avisaria.
    [Fact]
    public async Task DeleteFolders_WhenItIsTheLastOne_ShouldReturnBadRequest()
    {
        var sourceId = await ConnectAsync();

        var response = await _client.DeleteAsync(new Uri($"{Route}/{sourceId}/folders", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // A releitura deliberada zera o cursor de todas as pastas, e a varredura seguinte relê tudo.
    // É o que permite reavaliar o que já passou depois de cadastrar PayerProfile ou Payee — sem
    // eles, o que a cascata não reconhece é descartado em vez de ir para a quarentena.
    [Fact]
    public async Task PostRescan_ShouldClearEveryCursorSoTheNextSweepIsFull()
    {
        var sourceId = await ConnectAsync();
        await AddFolderAsync(sourceId, "Contas");
        await SyncAsync(sourceId);

        Assert.All((await LoadAsync(sourceId)).Folders, f => Assert.NotNull(f.SyncCursor));

        var response = await _client.PostAsync(new Uri($"{Route}/{sourceId}/rescan", UriKind.Relative), content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RescanCaptureSourceResponseDto>();
        Assert.Equal(2, body!.FoldersReset);

        Assert.All((await LoadAsync(sourceId)).Folders, f => Assert.Null(f.SyncCursor));

        _mailbox.Reads.Clear();
        await SyncAsync(sourceId);
        Assert.All(_mailbox.Reads, r => Assert.Null(r.Cursor));
    }

    // Reler não duplica: a ingestão é idempotente por (tenant, fonte, mensagem, anexo), então o
    // que já virou item continua o mesmo item.
    [Fact]
    public async Task PostRescan_ShouldNotDuplicateAlreadyIngestedArtifacts()
    {
        var sourceId = await ConnectAsync();
        _mailbox.ReadResult = MailboxReadResult.Ok(
            [FakeMailboxReader.Message("msg-1", "boleto.pdf")], "deltaLink-2", FakeMailboxReader.AttemptedAt);

        await SyncAsync(sourceId);

        await _client.PostAsync(new Uri($"{Route}/{sourceId}/rescan", UriKind.Relative), content: null);
        var depois = await SyncAsync(sourceId);

        Assert.Equal(0, depois.IngestedItems);
        Assert.Equal(1, depois.SkippedAsAlreadyIngested);
        Assert.Equal(1, await ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().CountAsync()));
    }

    // Reler fonte de outro tenant não encontra nada — isolamento sem exceção.
    [Fact]
    public async Task PostRescan_FromAnotherTenant_ShouldReturnNotFound()
    {
        var sourceId = await ConnectAsync();
        var outroTenant = new Guid("0195a1f0-0000-7000-8000-000000000002");

        var response = await _client.PostAsync(
            new Uri($"/api/v1/{outroTenant}/capture-sources/{sourceId}/rescan", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> ConnectAsync()
    {
        var payload = new ConnectCaptureSourceRequest(
            "MicrosoftGraphMailbox", "Caixa de contas a pagar", "contas@empresa.com.br", "segredo");

        var response = await _client.PostAsJsonAsync(Route, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>())!.Id;
    }

    private async Task AddFolderAsync(Guid sourceId, string folderPath)
    {
        var response = await _client.PostAsJsonAsync(
            new Uri($"{Route}/{sourceId}/folders", UriKind.Relative), new { folderPath });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<SyncCaptureSourceResponseDto> SyncAsync(Guid sourceId)
    {
        var response = await _client.PostAsync(new Uri($"{Route}/{sourceId}/sync", UriKind.Relative), content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<SyncCaptureSourceResponseDto>())!;
    }

    private Task<CaptureSource> LoadAsync(Guid id)
    {
        var sourceId = CaptureSourceId.From(id);
        return ExecuteDbContextAsync(db => db.CaptureSources.AsNoTracking().FirstAsync(s => s.Id == sourceId));
    }
}
