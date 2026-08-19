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
/// O ciclo completo da sprint 2.1: conectar, varrer, ingerir, e retomar de onde parou.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class SyncCaptureSourceTests : BaseIntegrationTest
{
    private static readonly Guid TenantA = new("0195a1f0-0000-7000-8000-000000000001");

    private readonly HttpClient _client;
    private readonly FakeMailboxReader _mailbox;

    public SyncCaptureSourceTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        var host = factory.WithReachableMailbox();
        _client = host.CreateClient().Authenticated();
        _mailbox = host.Services.GetRequiredService<FakeMailboxReader>();
    }

    private static Uri Route => new($"/api/v1/{TenantA}/capture-sources", UriKind.Relative);

    // Uma varredura ingere um CaptureItem por artefato — o e-mail com três boletos vira três itens.
    [Fact]
    public async Task PostSync_WithMessagesCarryingSeveralAttachments_ShouldIngestOneItemPerArtifact()
    {
        var sourceId = await ConnectAsync();
        _mailbox.ReadResult = MailboxReadResult.Ok(
            [FakeMailboxReader.Message("msg-1", "boleto-enel.pdf", "boleto-sabesp.pdf", "propaganda.pdf")],
            nextCursor: "deltaLink-2",
            FakeMailboxReader.AttemptedAt);

        var result = await SyncAsync(sourceId);

        Assert.Equal("Ok", result.Status);
        Assert.Equal(3, result.IngestedItems);

        var itens = await ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().CountAsync());
        Assert.Equal(3, itens);
    }

    // Reprocessar a caixa não duplica: o que já entrou é pulado, e nada de novo é criado.
    [Fact]
    public async Task PostSync_Twice_ShouldNotDuplicateAlreadyIngestedArtifacts()
    {
        var sourceId = await ConnectAsync();
        _mailbox.ReadResult = MailboxReadResult.Ok(
            [FakeMailboxReader.Message("msg-1", "boleto-enel.pdf", "boleto-sabesp.pdf")],
            nextCursor: "deltaLink-2",
            FakeMailboxReader.AttemptedAt);

        var primeira = await SyncAsync(sourceId);
        var segunda = await SyncAsync(sourceId);

        Assert.Equal(2, primeira.IngestedItems);
        Assert.Equal(0, segunda.IngestedItems);
        Assert.Equal(2, segunda.SkippedAsAlreadyIngested);

        var itens = await ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().CountAsync());
        Assert.Equal(2, itens);
    }

    // A sincronização seguinte retoma do cursor que a anterior gravou — é o que torna a varredura
    // incremental, e sem isso toda passagem releria a caixa inteira.
    [Fact]
    public async Task PostSync_ShouldAdvanceCursorAndResumeFromIt()
    {
        var sourceId = await ConnectAsync();
        _mailbox.ReadResult = MailboxReadResult.Ok([], nextCursor: "deltaLink-2", FakeMailboxReader.AttemptedAt);

        await SyncAsync(sourceId);
        Assert.Null(_mailbox.LastCursor);

        await SyncAsync(sourceId);
        Assert.Equal("deltaLink-2", _mailbox.LastCursor);

        var stored = await LoadAsync(sourceId);
        Assert.Equal("deltaLink-2", stored.Folders.First().SyncCursor);
        Assert.Null(stored.LastSyncError);
    }

    // Falha de leitura registra o erro e PRESERVA o cursor: avançá-lo pularia mensagens que
    // ninguém leu, e apagá-lo transformaria um timeout em varredura completa da caixa.
    [Fact]
    public async Task PostSync_WhenMailboxIsUnavailable_ShouldRecordErrorAndKeepCursor()
    {
        var sourceId = await ConnectAsync();
        await SyncAsync(sourceId);

        _mailbox.ReadResult = MailboxReadResult.Unavailable("throttled", "429", FakeMailboxReader.AttemptedAt);
        var result = await SyncAsync(sourceId);

        Assert.Equal("Unavailable", result.Status);

        var stored = await LoadAsync(sourceId);
        Assert.Equal("deltaLink-1", stored.Folders.First().SyncCursor);
        Assert.Equal("throttled", stored.LastSyncError);
    }

    // Cursor invalidado pelo provedor (410 Gone) é DESCARTADO, para a próxima varredura ser
    // completa — sem isso a fonte pararia de sincronizar em silêncio.
    [Fact]
    public async Task PostSync_WhenCursorExpired_ShouldDiscardCursorSoNextSweepIsFull()
    {
        var sourceId = await ConnectAsync();
        await SyncAsync(sourceId);

        _mailbox.ReadResult = MailboxReadResult.CursorExpired(
            "delta_token_expired", "410 Gone", FakeMailboxReader.AttemptedAt);

        var result = await SyncAsync(sourceId);

        Assert.Equal("CursorExpired", result.Status);

        var stored = await LoadAsync(sourceId);
        Assert.Null(stored.Folders.First().SyncCursor);
        Assert.Equal("delta_token_expired", stored.LastSyncError);

        // E a varredura seguinte de fato recomeça do zero.
        _mailbox.ReadResult = MailboxReadResult.Ok([], nextCursor: "deltaLink-novo", FakeMailboxReader.AttemptedAt);
        await SyncAsync(sourceId);
        Assert.Null(_mailbox.LastCursor);
    }

    // Fonte desativada recusa a sincronização — o botão de parada do usuário não é decorativo.
    [Fact]
    public async Task PostSync_WhenSourceIsDisabled_ShouldReturnConflict()
    {
        var sourceId = await ConnectAsync();

        var desativar = await _client.PutAsJsonAsync(
            new Uri($"{Route}/{sourceId}/activation", UriKind.Relative), new { isEnabled = false });
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var response = await _client.PostAsync(new Uri($"{Route}/{sourceId}/sync", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Sincronizar fonte de outro tenant não encontra nada — isolamento sem exceção.
    [Fact]
    public async Task PostSync_FromAnotherTenant_ShouldReturnNotFound()
    {
        var sourceId = await ConnectAsync();
        var outroTenant = new Guid("0195a1f0-0000-7000-8000-000000000002");

        var response = await _client.PostAsync(
            new Uri($"/api/v1/{outroTenant}/capture-sources/{sourceId}/sync", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> ConnectAsync()
    {
        var payload = new ConnectCaptureSourceRequest(
            "MicrosoftGraphMailbox", "Caixa de contas a pagar", "contas@empresa.com.br", "segredo");

        var response = await _client.PostAsJsonAsync(Route, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();
        return body!.Id;
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
