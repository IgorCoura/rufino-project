namespace BillPayment.IntegrationTests.CapturedMessages;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A recaptura pela porta da frente: puxar o e-mail de novo e refazer a triagem — sem passar por
/// cima de um boleto que já teve o pagamento autorizado.
/// </summary>
/// <remarks>
/// Até 2026-08-28 este handler tinha 0% de cobertura e três defeitos: apagava do balde o documento
/// de boleto promovido, devolvia um id que não existia mais, e corria contra o índice único dos
/// itens. Cada caso aqui fixa um deles.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RecaptureMessageTests : BaseIntegrationTest, IDisposable
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly TenantId OtherTenant = TenantId.From(TestTenants.Secondary);
    private static readonly UserId Requester = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a7"));
    private static readonly DateTime OccurredAt = new(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 8, 20);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string Sender = "faturas@fornecedor.com.br";
    private const string MessageId = "AAMkAGI2THVSAAA=";
    private const string InternetMessageId = "<recaptura-123@fornecedor.com.br>";
    private const string ArtifactKey = "anexo-boleto.pdf";

    private readonly WebApplicationFactory<Program> _host;
    private readonly IServiceProvider _services;
    private readonly FakeMailboxReader _reader;
    private readonly InMemoryAttachmentStorage _storage;

    public RecaptureMessageTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithCaptureChain();
        _services = _host.Services;
        _reader = _services.GetRequiredService<FakeMailboxReader>();
        _storage = _services.GetRequiredService<InMemoryAttachmentStorage>();
    }

    public void Dispose() => _host.Dispose();

    // O caminho feliz: o registro é o MESMO (id preservado), todo anexo volta a Pending, o item
    // volta a Received sem nada da triagem anterior, e o blob do item que nunca virou boleto sai
    // do balde — depois do commit.
    [Fact]
    public async Task Recapture_ShouldResetTheMessageAndItsItemsKeepingTheSameId()
    {
        var (messageId, itemId) = await SeedUnroutedAsync();
        ProviderReturns(ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecaptureMessageResponseContract>();
        Assert.Equal(messageId.Value, body!.Id);
        Assert.Equal(1, body.ArtifactsReingested);
        Assert.Equal(0, body.BillsCancelled);
        Assert.Empty(body.PreviouslyDeniedBillIds);

        var message = await LoadMessageAsync(messageId);
        Assert.NotNull(message);
        Assert.Null(message!.ProcessedAt);
        Assert.All(message.Artifacts, a => Assert.Equal(ArtifactOutcome.Pending, a.Outcome));

        var item = await LoadItemAsync(itemId);
        Assert.NotNull(item);
        Assert.Same(CaptureItemStatus.Received, item!.Status);
        Assert.Null(item.StorageKey);
        Assert.Null(item.Reason);
        Assert.Equal(0, _storage.Count);
    }

    // Boleto com pagamento autorizado trava a recaptura inteira: 409 BLP.CMS11, e NADA muda —
    // nem o item, nem o boleto, nem o arquivo no balde.
    [Fact]
    public async Task Recapture_WhenABillWasApproved_ShouldReturnConflictAndChangeNothing()
    {
        var (messageId, itemId, billId) = await SeedPromotedAsync(approve: true);
        ProviderReturns(ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.CMS11", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, item!.Status);
        Assert.Equal(billId, item.BillId);
        Assert.Same(BillStatus.Approved, (await LoadBillAsync(billId))!.Status);
        Assert.Equal(1, _storage.Count);
        Assert.Equal(ArtifactOutcome.Promoted, Assert.Single((await LoadMessageAsync(messageId))!.Artifacts).Outcome);
    }

    // Boleto ainda aguardando aprovação é cancelado por quem pediu a recaptura — a chave única do
    // instrumento é liberada e a triagem nova o recria. O arquivo dele fica: é a evidência do
    // boleto cancelado.
    [Fact]
    public async Task Recapture_WhenTheBillIsAwaitingApproval_ShouldCancelItAndKeepItsDocument()
    {
        var (messageId, itemId, billId) = await SeedPromotedAsync(approve: false);
        ProviderReturns(ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecaptureMessageResponseContract>();
        Assert.Equal(1, body!.BillsCancelled);

        var bill = await LoadBillAsync(billId);
        Assert.Same(BillStatus.Cancelled, bill!.Status);
        Assert.Equal(Requester, bill.Approval!.DecidedBy);

        Assert.Same(CaptureItemStatus.Received, (await LoadItemAsync(itemId))!.Status);
        Assert.Equal(1, _storage.Count);
    }

    // Boleto negado não bloqueia nem é cancelado — mas quem pediu fica sabendo que aquele boleto
    // já tinha sido negado uma vez, porque ele vai renascer para decisão de novo.
    [Fact]
    public async Task Recapture_WhenTheBillWasDenied_ShouldProceedAndReportIt()
    {
        var (messageId, itemId, billId) = await SeedPromotedAsync(approve: false, deny: true);
        ProviderReturns(ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecaptureMessageResponseContract>();
        Assert.Equal(0, body!.BillsCancelled);
        Assert.Equal(billId.Value, Assert.Single(body.PreviouslyDeniedBillIds));

        Assert.Same(BillStatus.Denied, (await LoadBillAsync(billId))!.Status);
        Assert.Same(CaptureItemStatus.Received, (await LoadItemAsync(itemId))!.Status);
    }

    // O provedor não tem mais a mensagem: 404 BLP.CMS12 e nada foi tocado — a busca vem ANTES de
    // qualquer mutação.
    [Fact]
    public async Task Recapture_WhenTheProviderNoLongerHasTheMessage_ShouldReturnNotFoundAndChangeNothing()
    {
        var (messageId, itemId) = await SeedUnroutedAsync();

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("BLP.CMS12", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrouted, item!.Status);
        Assert.NotNull(item.StorageKey);
        Assert.Equal(1, _storage.Count);
    }

    // Os anexos são sincronizados com o que o provedor devolve AGORA: o que sumiu sai (item e
    // registro), o novo entra em Received, o que ficou é reescrito — sem colidir no índice único.
    [Fact]
    public async Task Recapture_ShouldSyncTheArtifactsWithWhatTheProviderReturnsNow()
    {
        var (messageId, itemId) = await SeedUnroutedAsync();
        ProviderReturns("anexo-novo.pdf", ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<RecaptureMessageResponseContract>())!.ArtifactsReingested);

        var message = await LoadMessageAsync(messageId);
        Assert.Equal(2, message!.ArtifactCount);
        Assert.Contains(message.Artifacts, a => a.ArtifactKey == "anexo-novo.pdf");

        var items = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .Where(i => i.TenantId == Tenant)
            .ToListAsync());

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Id == itemId && i.ArtifactKey == ArtifactKey);
        Assert.Contains(items, i => i.ArtifactKey == "anexo-novo.pdf");
        Assert.All(items, i => Assert.Same(CaptureItemStatus.Received, i.Status));
    }

    // O anexo que o provedor não devolve mais some com o item, e o blob dele sai do balde.
    [Fact]
    public async Task Recapture_WhenAnArtifactVanishedAtTheProvider_ShouldRemoveItsItem()
    {
        var (messageId, itemId) = await SeedUnroutedAsync();
        ProviderReturns("outro-anexo.pdf");

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await LoadItemAsync(itemId));
        Assert.DoesNotContain((await LoadMessageAsync(messageId))!.Artifacts, a => a.ArtifactKey == ArtifactKey);
        Assert.Equal(0, _storage.Count);
    }

    // Idempotência: o mesmo x-requestid não recaptura duas vezes.
    [Fact]
    public async Task Recapture_ResentWithTheSameRequestId_ShouldRunOnce()
    {
        var (messageId, _) = await SeedUnroutedAsync();
        ProviderReturns(ArtifactKey);
        var requestId = new Guid("0195a1f0-0000-7000-8000-00000000ee01");

        var first = await RecaptureAsync(messageId, requestId);
        var second = await RecaptureAsync(messageId, requestId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(Guid.Empty, (await second.Content.ReadFromJsonAsync<RecaptureMessageResponseContract>())!.Id);
    }

    // Registro de outro tenant não existe para este — 404, sem distinguir de "nunca existiu".
    [Fact]
    public async Task Recapture_OfAnotherTenantsMessage_ShouldReturnNotFound()
    {
        var (messageId, _) = await SeedUnroutedAsync(OtherTenant);
        ProviderReturns(ArtifactKey);

        var response = await RecaptureAsync(messageId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private void ProviderReturns(params string[] artifactKeys)
    {
        _reader.Relocations[InternetMessageId] = new RelocatedArtifact(MessageId, artifactKeys[0]);
        _reader.MessagesById[MessageId] = MailboxMessage.From(
            MessageId,
            Sender,
            "Segunda via do boleto",
            new DateTimeOffset(OccurredAt.AddHours(-2)),
            artifactKeys.Select(key => MailboxArtifact.From(key, key, "application/pdf", 1024)),
            InternetMessageId);
    }

    private Task<HttpResponseMessage> RecaptureAsync(CapturedMessageId messageId, Guid? requestId = null)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/{Tenant.Value}/captured-messages/{messageId.Value}/recapture");

        request.Headers.Add("x-user-id", Requester.Value.ToString());
        request.Headers.Add("x-requestid", (requestId ?? Guid.CreateVersion7()).ToString());
        request.Content = JsonContent.Create(new { });

        return _host.CreateClient().Authenticated().SendAsync(request);
    }

    /// <summary>Registro + item já triado (Unrouted, com arquivo no balde), sem boleto.</summary>
    private async Task<(CapturedMessageId MessageId, CaptureItemId ItemId)> SeedUnroutedAsync(TenantId? tenant = null)
    {
        var tenantId = tenant ?? Tenant;
        var sourceId = await SeedSourceAsync(tenantId);
        var storageKey = await _storage.StoreAsync(tenantId, ArtifactKey, "application/pdf", new byte[] { 1, 2, 3 }, CancellationToken.None);

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                tenantId, sourceId, MessageId, ArtifactKey, Sender, "Boleto", OccurredAt, OccurredAt,
                "application/pdf", "boleto.pdf", InternetMessageId);
            item.StoreArtifact("sha256:abc", storageKey, OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            item.MarkUnrouted("payer_not_identified", OccurredAt);

            var message = CapturedMessage.Register(
                tenantId, sourceId, MessageId, Sender, "Boleto", OccurredAt, OccurredAt,
                [(ArtifactKey, "boleto.pdf", "application/pdf")], InternetMessageId);
            message.RecordOutcome(ArtifactKey, ArtifactOutcome.Unrouted, "payer_not_identified", item.Id, null, OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.CapturedMessages.AddAsync(message);
            await db.SaveEntitiesAsync();
            return (message.Id, item.Id);
        });
    }

    /// <summary>Registro + item promovido a boleto — aguardando aprovação, aprovado ou negado.</summary>
    private async Task<(CapturedMessageId MessageId, CaptureItemId ItemId, BillId BillId)> SeedPromotedAsync(
        bool approve, bool deny = false)
    {
        var sourceId = await SeedSourceAsync(Tenant);
        var storageKey = await _storage.StoreAsync(Tenant, ArtifactKey, "application/pdf", new byte[] { 1, 2, 3 }, CancellationToken.None);

        return await ExecuteDbContextAsync(async db =>
        {
            var line = DigitableLine.Parse(BankSlipLine, OccurredAt);
            var bill = Bill.Capture(
                Tenant,
                [PaymentInstrument.FromBarcode(line)],
                BillOrigin.Create(BillSourceKind.Mailbox, OccurredAt, sourceId.Value, Sender, MessageId, "sha256:abc", storageKey),
                OccurredAt);

            var consultedAt = new DateTimeOffset(OccurredAt);
            bill.AttachLookups(
                BillLookupResult.Resolved(
                    LookupSnapshot.Create(
                        LookupParty.From("PADARIA SAO JOSE LTDA", null, "11222333000181"),
                        consultedAt,
                        bankCode: line.BankCode,
                        amount: line.Amount,
                        originalAmount: line.Amount,
                        dueDate: line.DueDate is { } due ? DateOnly.FromDateTime(due) : null,
                        fee: null),
                    consultedAt),
                null,
                OccurredAt);
            bill.RecordChecks([.. Enumeration.GetAll<CheckType>().Select(t => CheckResult.Passed(t))], OccurredAt);

            if (approve)
            {
                // O boleto sintético está vencido: o ADR-017 exige o aceite de execução
                // imediata para aprovar vencido (BLP.BIL35).
                bill.Approve(
                    Requester, Today.AddDays(3), null, ApprovalPolicy.Default(null),
                    RiskLevel.ExtremeDanger, Today, OccurredAt,
                    acknowledgeRisk: false, acknowledgeImmediateExecution: true);
            }
            else if (deny)
                bill.Deny(Requester, "duplicado", OccurredAt);

            var item = CaptureItem.Ingest(
                Tenant, sourceId, MessageId, ArtifactKey, Sender, "Boleto", OccurredAt, OccurredAt,
                "application/pdf", "boleto.pdf", InternetMessageId);
            item.StoreArtifact("sha256:abc", storageKey, OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            item.Promote(bill.Id, RoutingConfidence.Strong, OccurredAt);

            var message = CapturedMessage.Register(
                Tenant, sourceId, MessageId, Sender, "Boleto", OccurredAt, OccurredAt,
                [(ArtifactKey, "boleto.pdf", "application/pdf")], InternetMessageId);
            message.RecordOutcome(ArtifactKey, ArtifactOutcome.Promoted, null, item.Id, bill.Id, OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.CaptureItems.AddAsync(item);
            await db.CapturedMessages.AddAsync(message);
            await db.SaveEntitiesAsync();
            return (message.Id, item.Id, bill.Id);
        });
    }

    private Task<CaptureSourceId> SeedSourceAsync(TenantId tenantId)
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                tenantId,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c9")),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

    private Task<CapturedMessage?> LoadMessageAsync(CapturedMessageId id)
        => ExecuteDbContextAsync(db => db.CapturedMessages
            .AsNoTracking()
            .Include(m => m.Artifacts)
            .FirstOrDefaultAsync(m => m.Id == id));

    private Task<CaptureItem?> LoadItemAsync(CaptureItemId id)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id));

    private Task<Bill?> LoadBillAsync(BillId id)
        => ExecuteDbContextAsync(db => db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id));

    private sealed record RecaptureMessageResponseContract(
        Guid Id,
        int ArtifactsReingested,
        int BillsCancelled,
        Guid[] PreviouslyDeniedBillIds);
}
