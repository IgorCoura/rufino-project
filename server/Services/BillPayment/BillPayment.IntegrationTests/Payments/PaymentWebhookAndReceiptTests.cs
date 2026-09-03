namespace BillPayment.IntegrationTests.Payments;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A volta da verdade do provedor (3.3): o webhook autenticado e idempotente refletindo no
/// espelho, o comprovante baixado e guardado como arquivo, e a reabertura do falhado (3.4).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class PaymentWebhookAndReceiptTests : BaseIntegrationTest, IDisposable
{
    private const string WebhookToken = "tok-webhook-teste";

    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid ApproverId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly FakePaymentGateways _gateways;
    private readonly FakeReceiptFetcher _receipts;
    private readonly HttpClient _client;

    public PaymentWebhookAndReceiptTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithPaymentChain()
            .WithWebHostBuilder(builder => builder.UseSetting("PaymentWebhook:Token", WebhookToken));

        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
        _gateways = _host.Services.GetRequiredService<FakePaymentGateways>();
        _receipts = _host.Services.GetRequiredService<FakeReceiptFetcher>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _gateways.Reset();
        _receipts.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    // Sem token configurado o webhook nem existe: um caminho anônimo aberto para mexer em
    // ordens de pagamento seria pior que não ter webhook.
    [Fact]
    public async Task Webhook_WithoutAConfiguredToken_ShouldRespond404()
    {
        using var bare = Factory.WithPaymentChain();
        using var client = bare.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/webhooks/asaas", UriKind.Relative),
            new { id = "evt_1", @event = "BILL_PAID" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Token errado é 401 — validado em tempo constante, antes de olhar o corpo.
    [Fact]
    public async Task Webhook_WithTheWrongToken_ShouldRespond401()
    {
        var response = await PostWebhookAsync(
            new { id = "evt_2", @event = "BILL_PAID" }, token: "token-errado");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // O caminho feliz: BILL_PAID chega, a ordem vira Paid e o boleto espelha — e o comprovante
    // é baixado e guardado como ARQUIVO, servido depois pelo endpoint próprio.
    [Fact]
    public async Task Webhook_Paid_ShouldMirrorTheBillAndStoreTheReceipt()
    {
        var (billId, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));

        var response = await PostWebhookAsync(new
        {
            id = "evt_paid_1",
            @event = "BILL_PAID",
            bill = new
            {
                id = "pay_fake_1",
                status = "PAID",
                externalReference = orderId.ToString(),
                paymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await DrainOutboxAsync();

        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        Assert.False(string.IsNullOrEmpty(order.ReceiptStorageKey));
        Assert.Equal("https://www.asaas.com/comprovantes/000123", _receipts.LastUrl);

        Assert.Equal(BillStatus.Paid, (await LoadBillAsync(billId)).Status);

        var receipt = await _client.GetAsync(
            new Uri($"/api/v1/{TenantId}/payments/{orderId}/receipt", UriKind.Relative), CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        Assert.Equal(FakeReceiptFetcher.DefaultReceipt, await receipt.Content.ReadAsByteArrayAsync(CancellationToken.None));
    }

    // A idempotência por id de evento: a reentrega do mesmo evento não produz efeito nenhum.
    [Fact]
    public async Task Webhook_Redelivered_ShouldHaveNoSecondEffect()
    {
        var (_, orderId) = await SubmitOrderAsync();
        var payload = new
        {
            id = "evt_dup_1",
            @event = "BILL_BANK_PROCESSING",
            bill = new { externalReference = orderId.ToString() },
        };

        var first = await PostWebhookAsync(payload);
        var second = await PostWebhookAsync(payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("Duplicate", await second.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);

        var ledgerRows = await ExecuteDbContextAsync(db => db.PaymentWebhookEvents.AsNoTracking().CountAsync());
        Assert.Equal(1, ledgerRows);
        Assert.Equal(PaymentOrderStatus.BankProcessing, (await LoadOrderAsync(orderId)).Status);
    }

    // Referência que não é nossa devolve 200: falhar faria o provedor reentregar para sempre um
    // evento de outra conta.
    [Fact]
    public async Task Webhook_WithAnUnknownReference_ShouldAcknowledgeWithoutEffect()
    {
        var response = await PostWebhookAsync(new
        {
            id = "evt_unknown_1",
            @event = "BILL_PAID",
            bill = new { externalReference = Guid.NewGuid().ToString() },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Unknown", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
    }

    // O falhado reabre pela borda e a nova aprovação cria uma ORDEM NOVA — a falhada fica como
    // história (ADR-002).
    [Fact]
    public async Task Reopen_AFailedBill_ShouldAllowANewApprovalWithANewOrder()
    {
        var (billId, orderId) = await SubmitOrderAsync(submit: false);
        await ClaimAsync();
        _gateways.ScriptedSubmission = PaymentSubmissionResult.Refused("invalid_bank_slip", null);
        await SubmitCommandAsync(orderId);
        await DrainOutboxAsync();
        Assert.Equal(BillStatus.Failed, (await LoadBillAsync(billId)).Status);

        var reopen = await PostBillAsync($"{billId}/reopen");
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);

        var bill = await LoadBillAsync(billId);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Null(bill.PaymentOrderId);

        _gateways.ScriptedSubmission = null;
        var approve = await PostBillAsync(
            $"{billId}/approve",
            new ApproveBillRequest(ScheduleDate(), null, AcknowledgeRisk: true));
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        await DrainOutboxAsync();

        var newOrder = await ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .Where(o => o.BillId == BillId.From(billId) && o.Status == PaymentOrderStatus.Draft)
            .SingleAsync());
        Assert.NotEqual(orderId, newOrder.Id.Value);
    }

    // O objeto do payload pode chegar como "payment" em vez de "bill" — o contrato é medido, a
    // sonda está bloqueada, e a leitura frouxa aceita as duas formas.
    [Fact]
    public async Task Webhook_WithAPaymentObjectPayload_ShouldStillResolveTheOrder()
    {
        var (_, orderId) = await SubmitOrderAsync();

        var response = await PostWebhookAsync(new
        {
            id = "evt_payment_shape_1",
            @event = "BILL_BANK_PROCESSING",
            payment = new { externalReference = orderId.ToString() },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentOrderStatus.BankProcessing, (await LoadOrderAsync(orderId)).Status);
    }

    // Evento que o provedor inventar amanhã cai em Pending pela monotônica — 200 Ignored, nunca
    // desfecho por chute nem erro que faria o provedor reentregar para sempre.
    [Fact]
    public async Task Webhook_WithAnUnknownEventName_ShouldAcknowledgeWithoutEffect()
    {
        var (_, orderId) = await SubmitOrderAsync();

        var response = await PostWebhookAsync(new
        {
            id = "evt_new_kind_1",
            @event = "BILL_SOMETHING_NEW",
            bill = new { externalReference = orderId.ToString() },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ignored", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(PaymentOrderStatus.Pending, (await LoadOrderAsync(orderId)).Status);
    }

    // Payload sem id ou sem event não tem como entrar no ledger de idempotência — 400, e o
    // provedor que manda isso tem um defeito que precisa aparecer do lado dele.
    [Theory]
    [InlineData("""{"event":"BILL_PAID"}""")]
    [InlineData("""{"id":"evt_no_event"}""")]
    [InlineData("{}")]
    public async Task Webhook_WithoutAnIdOrEvent_ShouldRespond400(string payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/webhooks/asaas", UriKind.Relative))
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("asaas-access-token", WebhookToken);

        var response = await _client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Falha PASSAGEIRA no download do comprovante sobe como BLP.PMO21 — é o sinal que devolve o
    // trabalho à reentrega do outbox; o pagamento fica Paid e nada é gravado pela metade.
    [Fact]
    public async Task CaptureReceipt_WhenTheFetchIsRetryable_ShouldThrowPmo21WithoutStoringAnything()
    {
        var (_, orderId) = await SubmitOrderAsync();
        await MarkPaidAsync(orderId);
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));
        _receipts.Scripted = ReceiptFetchResult.Unavailable("http_503");

        var thrown = await Assert.ThrowsAsync<DomainException>(() => CaptureReceiptAsync(orderId));

        Assert.Equal("BLP.PMO21", thrown.Id);
        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        Assert.Null(order.ReceiptStorageKey);
    }

    // Pagamento sem comprovante no provedor é DESFECHO (NoReceipt), nunca falha: o dinheiro já
    // saiu, e falhar aqui não o traria de volta. O endpoint então responde 404, colapsado.
    [Fact]
    public async Task CaptureReceipt_WhenTheProviderOffersNoUrl_ShouldRecordNoReceiptAndServe404()
    {
        var (_, orderId) = await SubmitOrderAsync();
        await MarkPaidAsync(orderId);

        var outcome = await CaptureReceiptAsync(orderId);

        Assert.Equal("NoReceipt", outcome);
        Assert.Equal(PaymentOrderStatus.Paid, (await LoadOrderAsync(orderId)).Status);

        var receipt = await _client.GetAsync(
            new Uri($"/api/v1/{TenantId}/payments/{orderId}/receipt", UriKind.Relative), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, receipt.StatusCode);
    }

    // A reentrega do outbox depois do comprovante guardado é AlreadyStored — um blob só no
    // balde, nunca um segundo download.
    [Fact]
    public async Task CaptureReceipt_Redelivered_ShouldNotStoreASecondBlob()
    {
        var (_, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));

        await PostWebhookAsync(new
        {
            id = "evt_receipt_dup_1",
            @event = "BILL_PAID",
            bill = new
            {
                externalReference = orderId.ToString(),
                paymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            },
        });
        await DrainOutboxAsync();

        var storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
        var blobsAfterFirst = storage.Count;
        Assert.Equal(1, _receipts.Calls);

        var outcome = await CaptureReceiptAsync(orderId);

        Assert.Equal("AlreadyStored", outcome);
        Assert.Equal(blobsAfterFirst, storage.Count);
        Assert.Equal(1, _receipts.Calls);
    }

    // O comprovante é do tenant: a MESMA pessoa com acesso a duas contas não alcança a ordem de
    // um tenant pela rota do outro — 404 colapsado, como toda negativa de artefato.
    [Fact]
    public async Task Receipt_ThroughAnotherTenantsRoute_ShouldRespond404()
    {
        var (_, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));
        await PostWebhookAsync(new
        {
            id = "evt_cross_tenant_1",
            @event = "BILL_PAID",
            bill = new
            {
                externalReference = orderId.ToString(),
                paymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            },
        });
        await DrainOutboxAsync();

        var own = await _client.GetAsync(
            new Uri($"/api/v1/{TenantId}/payments/{orderId}/receipt", UriKind.Relative), CancellationToken.None);
        var foreign = await _client.GetAsync(
            new Uri($"/api/v1/{TestTenants.Secondary}/payments/{orderId}/receipt", UriKind.Relative),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    // Reabrir um boleto que não falhou é conflito — reabertura não é atalho para desfazer aprovação.
    [Fact]
    public async Task Reopen_AnApprovedBill_ShouldReturn409()
    {
        var (billId, _) = await SubmitOrderAsync(submit: false);

        var response = await PostBillAsync($"{billId}/reopen");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL34", await response.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
    }

    private static DateOnly ScheduleDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

    /// <summary>Boleto importado, validado, aprovado e (opcionalmente) submetido.</summary>
    private async Task<(Guid BillId, Guid OrderId)> SubmitOrderAsync(bool submit = true)
    {
        await LinkPaymentAccountAsync();

        _lookups.BankSlipResult = FutureDueSnapshot();
        var import = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/bills/import", UriKind.Relative),
            new ImportBillRequest(BankSlipLine, null, "ManualUpload", ReceivedAt),
            CancellationToken.None);
        import.EnsureSuccessStatusCode();
        var billId = (await import.Content.ReadFromJsonAsync<ImportBillResponseContract>(CancellationToken.None))!.Id;
        await DrainOutboxAsync();

        var approve = await PostBillAsync(
            $"{billId}/approve", new ApproveBillRequest(ScheduleDate(), null, AcknowledgeRisk: true));
        approve.EnsureSuccessStatusCode();
        await DrainOutboxAsync();

        var orderId = (await ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.BillId == BillId.From(billId)))).Id.Value;

        if (submit)
        {
            await ClaimAsync();
            await SubmitCommandAsync(orderId);
            await DrainOutboxAsync();
        }

        return (billId, orderId);
    }

    private static BillLookupResult FutureDueSnapshot()
    {
        var at = DateTimeOffset.UtcNow;
        var line = DigitableLine.Parse(BankSlipLine, DateTime.UtcNow);

        return BillLookupResult.Resolved(
            LookupSnapshot.Create(
                LookupParty.From("PADARIA SAO JOSE LTDA", null, "11222333000181"),
                at,
                bankCode: line.BankCode,
                amount: line.Amount,
                originalAmount: line.Amount,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20)),
            at);
    }

    private async Task LinkPaymentAccountAsync()
    {
        var register = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile", UriKind.Relative),
            new RegisterPayerProfileRequest("Company", "RUFINO", "11444777000161"),
            CancellationToken.None);
        register.EnsureSuccessStatusCode();

        var link = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/{TenantId}/payer-profile/asaas-account", UriKind.Relative),
            new LinkAsaasAccountRequest("$aact_test_chave_do_tenant"),
            CancellationToken.None);
        link.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> PostWebhookAsync<T>(T payload, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/webhooks/asaas", UriKind.Relative))
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add("asaas-access-token", token ?? WebhookToken);

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task<HttpResponseMessage> PostBillAsync(string path, object? payload = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri($"/api/v1/{TenantId}/bills/{path}", UriKind.Relative))
        {
            Content = payload is null ? null : JsonContent.Create(payload),
        };

        request.Headers.Add("x-user-id", ApproverId.ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task ClaimAsync()
    {
        using var scope = _host.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        await queries.ClaimPendingSubmissionsAsync(10, DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);
    }

    private async Task SubmitCommandAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new SubmitPaymentOrderCommand(TenantId, orderId), CancellationToken.None);
    }

    private async Task DrainOutboxAsync()
    {
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        while (await processor.ProcessPendingAsync(CancellationToken.None) > 0)
        {
        }
    }

    /// <summary>
    /// Marca a ordem como paga SEM drenar o outbox — o teste então dirige a captura do
    /// comprovante pelo comando, deterministicamente, em vez de pela reentrega.
    /// </summary>
    private async Task MarkPaidAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IPaymentOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = await orders.GetAsync(
            Domain.SharedKernel.TenantId.From(TenantId), PaymentOrderId.From(orderId), CancellationToken.None);

        order!.ApplyProviderStatus(
            PaymentOrderStatus.Paid, DateOnly.FromDateTime(DateTime.UtcNow), null, null,
            DateTimeOffset.UtcNow, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(CancellationToken.None);
    }

    private async Task<string> CaptureReceiptAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(
            new CapturePaymentReceiptCommand(TenantId, orderId), CancellationToken.None);
        return response.Outcome;
    }

    private Task<PaymentOrder> LoadOrderAsync(Guid orderId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.Id == PaymentOrderId.From(orderId)));

    private Task<Bill> LoadBillAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .SingleAsync(b => b.Id == BillId.From(billId)));
}
