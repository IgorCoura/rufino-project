namespace BillPayment.IntegrationTests.CapturedMessages;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using BillPayment.Application.CaptureSources.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Retention.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Retention;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O corpo do e-mail retido na sincronização e servido pela API — a base do "ver e-mail" e a
/// entrada de corpo da extração por IA (Fase B de 2026-08-27).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CapturedMessageBodyFlowTests : BaseIntegrationTest, IDisposable
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly DateTime OccurredAt = new(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 27, 8, 0, 0, TimeSpan.Zero);

    private const string MessageId = "AAMkAGI2THVSAAA=";
    private const string Sender = "faturas@fornecedor.com.br";
    private const string BodyHtml = "<html><body><p>Sua fatura chegou.</p></body></html>";

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeMailboxReader _mailbox;
    private readonly InMemoryAttachmentStorage _storage;
    private readonly HttpClient _client;

    public CapturedMessageBodyFlowTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithCaptureChain();
        _mailbox = _host.Services.GetRequiredService<FakeMailboxReader>();
        _storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    // A sincronização guarda o corpo do e-mail no balde e registra a chave no livro-caixa; a
    // API serve o corpo pela rota do registro.
    [Fact]
    public async Task Sync_WithABody_ShouldStoreItAndServeItThroughTheApi()
    {
        var sourceId = await SeedSourceAsync();
        ProgramMailboxWithOneMessage();

        await SyncAsync(sourceId);

        var message = await LoadMessageAsync();
        Assert.NotNull(message);
        Assert.True(message!.HasStoredBody);
        Assert.Equal("text/html", message.BodyContentType);
        Assert.True(_storage.Contains(message.BodyStorageKey!));

        var response = await _client.GetAsync(
            new Uri($"/api/v1/{Tenant.Value}/captured-messages/{message.Id.Value}/body", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BodyContract>();
        Assert.Equal(BodyHtml, body!.Content);
        Assert.Equal(Sender, body.Sender);
        Assert.Equal("text/html", body.ContentType);
    }

    // Corpo indisponível no provedor não derruba a varredura: a mensagem entra sem corpo, e o
    // registro continua servindo o cabeçalho.
    [Fact]
    public async Task Sync_WhenTheBodyDownloadFails_ShouldStillRegisterTheMessage()
    {
        var sourceId = await SeedSourceAsync();
        ProgramMailboxWithOneMessage(includeBodyBytes: false);

        await SyncAsync(sourceId);

        var message = await LoadMessageAsync();
        Assert.NotNull(message);
        Assert.False(message!.HasStoredBody);
    }

    // O e-mail de um boleto vindo de caixa é alcançável pela rota do próprio boleto.
    [Fact]
    public async Task GetBillEmail_OnAMailboxBill_ShouldReturnTheStoredBody()
    {
        var sourceId = await SeedSourceAsync();
        ProgramMailboxWithOneMessage();
        await SyncAsync(sourceId);

        var message = await LoadMessageAsync();
        var billId = await SeedMailboxBillAsync(sourceId);

        var response = await _client.GetAsync(
            new Uri($"/api/v1/{Tenant.Value}/bills/{billId}/email", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BodyContract>();
        Assert.Equal(BodyHtml, body!.Content);
        Assert.Equal(message!.Id.Value, body.Id);
    }

    // O item da quarentena alcança o e-mail que o trouxe pela rota própria — é o que deixa a
    // pessoa ler o contexto antes de reivindicar ou reprovar.
    [Fact]
    public async Task GetCaptureItemEmail_OnAMailboxItem_ShouldReturnTheStoredBody()
    {
        var sourceId = await SeedSourceAsync();
        ProgramMailboxWithOneMessage();
        await SyncAsync(sourceId);

        // A própria sincronização ingeriu o item do anexo — o teste usa o item real da fila.
        var itemId = await ExecuteDbContextAsync(async db =>
            (await db.CaptureItems.AsNoTracking()
                .SingleAsync(i =>
                    i.TenantId == Tenant && i.ExternalMessageId == MessageId && i.ArtifactKey == "anexo-1")).Id);

        var response = await _client.GetAsync(
            new Uri($"/api/v1/{Tenant.Value}/capture-items/{itemId.Value}/email", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BodyContract>();
        Assert.Equal(BodyHtml, body!.Content);
        Assert.Equal(Sender, body.Sender);
    }

    // Item cuja mensagem não está no livro-caixa (nem alcançável no provedor) devolve 404 — a
    // mesma resposta fechada dos demais recursos.
    [Fact]
    public async Task GetCaptureItemEmail_WithoutARegisteredMessage_ShouldReturnNotFound()
    {
        var sourceId = await SeedSourceAsync();
        var itemId = await SeedCaptureItemAsync(sourceId, externalMessageId: "AAMkOUTRA=");

        var response = await _client.GetAsync(
            new Uri($"/api/v1/{Tenant.Value}/capture-items/{itemId.Value}/email", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Boleto importado à mão não tem e-mail por trás — 404, estado normal.
    [Fact]
    public async Task GetBillEmail_OnAManualBill_ShouldReturnNotFound()
    {
        var billId = await SeedManualBillAsync();

        var response = await _client.GetAsync(
            new Uri($"/api/v1/{Tenant.Value}/bills/{billId}/email", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // A purga leva o corpo junto com o registro vencido — reter o blob de um e-mail purgado
    // seria manter exatamente o que a janela mandou apagar.
    [Fact]
    public async Task Purge_ShouldRemoveTheStoredBodyBlobAlongWithTheRecord()
    {
        var sourceId = await SeedSourceAsync();
        ProgramMailboxWithOneMessage(receivedAt: DateTimeOffset.UtcNow.AddYears(-1));
        await SyncAsync(sourceId);

        var message = await LoadMessageAsync();
        var bodyKey = message!.BodyStorageKey!;
        Assert.True(_storage.Contains(bodyKey));

        await SeedRetentionAsync();
        var purged = await PurgeAsync();

        Assert.Equal(1, purged);
        Assert.False(_storage.Contains(bodyKey));
        Assert.Null(await LoadMessageAsync());
    }

    private void ProgramMailboxWithOneMessage(bool includeBodyBytes = true, DateTimeOffset? receivedAt = null)
    {
        var artifacts = new List<MailboxArtifact>
        {
            MailboxArtifact.From("anexo-1", "boleto.pdf", "application/pdf", 1234),
            MailboxArtifact.From(IMailboxReader.BODY_ARTIFACT_KEY, null, "text/html", BodyHtml.Length),
        };

        var message = MailboxMessage.From(
            MessageId, Sender, "Sua fatura chegou", receivedAt ?? ReceivedAt, artifacts, "<m1@fornecedor>");

        _mailbox.ReadResult = MailboxReadResult.Ok([message], "deltaLink-2", FakeMailboxReader.AttemptedAt);

        if (includeBodyBytes)
            _mailbox.Artifacts[IMailboxReader.BODY_ARTIFACT_KEY] = Encoding.UTF8.GetBytes(BodyHtml);
        else
            _mailbox.Artifacts.Remove(IMailboxReader.BODY_ARTIFACT_KEY);
    }

    private async Task SyncAsync(CaptureSourceId sourceId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new SyncCaptureSourceCommand(Tenant.Value, sourceId.Value));
    }

    private async Task<int> PurgeAsync()
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new PurgeCapturedMessagesCommand(Tenant.Value, 100));
        return result.Purged;
    }

    private Task<CapturedMessage?> LoadMessageAsync()
        => ExecuteDbContextAsync(db => db.CapturedMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == Tenant && m.ExternalMessageId == MessageId));

    private Task<CaptureSourceId> SeedSourceAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(Guid.CreateVersion7()),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

    private Task<BillPayment.Domain.CaptureItems.CaptureItemId> SeedCaptureItemAsync(
        CaptureSourceId sourceId,
        string? externalMessageId = null)
        => ExecuteDbContextAsync(async db =>
        {
            var item = BillPayment.Domain.CaptureItems.CaptureItem.Ingest(
                Tenant, sourceId, externalMessageId ?? MessageId, "anexo-1", Sender, "Sua fatura chegou",
                ReceivedAt.UtcDateTime, OccurredAt, "application/pdf", "boleto.pdf");

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

    private Task<Guid> SeedMailboxBillAsync(CaptureSourceId sourceId)
        => ExecuteDbContextAsync(async db =>
        {
            var bill = Bill.Capture(
                Tenant,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(BankSlipLine, OccurredAt))],
                BillOrigin.Create(
                    BillSourceKind.Mailbox,
                    ReceivedAt.UtcDateTime,
                    sourceId: sourceId.Value,
                    senderAddress: Sender,
                    externalMessageId: MessageId),
                OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id.Value;
        });

    private Task<Guid> SeedManualBillAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var bill = Bill.Capture(
                Tenant,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(BankSlipLine, OccurredAt))],
                BillOrigin.Create(
                    BillSourceKind.ManualUpload,
                    ReceivedAt.UtcDateTime,
                    storageKey: "tenant/2026-08/boleto.pdf"),
                OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id.Value;
        });

    private Task SeedRetentionAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);
            policy.Configure(true, RetentionWindow.SevenDays, OccurredAt);
            await db.CaptureRetentionPolicies.AddAsync(policy);
            await db.SaveEntitiesAsync();
        });

    private sealed record BodyContract(
        Guid Id,
        string Sender,
        string? Subject,
        DateTime ReceivedAt,
        string ContentType,
        string Content);
}
