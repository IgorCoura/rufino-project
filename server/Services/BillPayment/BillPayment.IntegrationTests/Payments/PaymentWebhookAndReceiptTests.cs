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
    private readonly FakePaymentWebhookProvisioner _provisioner;
    private readonly HttpClient _client;

    public PaymentWebhookAndReceiptTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithPaymentChain();

        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
        _gateways = _host.Services.GetRequiredService<FakePaymentGateways>();
        _receipts = _host.Services.GetRequiredService<FakeReceiptFetcher>();
        _provisioner = _host.Services.GetRequiredService<FakePaymentWebhookProvisioner>();

        // O token da INSTALAÇÃO deixou de existir em 2026-09-08 (ADR-019): cada conta tem o seu,
        // gerado no provisionamento. Aqui ele é ARMADO para que o teste possa apresentá-lo no
        // header — no fluxo real ele só existe cifrado no cofre.
        _provisioner.NextEnsure = WebhookProvisioningResult.Provisioned("wh_teste", WebhookToken);

        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _gateways.Reset();
        _receipts.Reset();
        _provisioner.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    // Tenant sem webhook provisionado responde 200 `NotConfigured`, e isso é DELIBERADO
    // (ADR-022): o provedor só considera sucesso o HTTP 200 e interrompe a fila SEQUENCIAL da
    // conta após 15 falhas consecutivas, descartando o represado em 14 dias. Devolver erro aqui
    // faria um desvínculo pausar a conta do cliente em quinze entregas — e nenhuma delas é
    // forjadura: é o provedor esvaziando a fila dele.
    [Fact]
    public async Task Webhook_ForATenantWithoutAWebhook_ShouldAbsorbWith200()
    {
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri($"/webhooks/asaas/{TenantId}", UriKind.Relative),
            new { id = "evt_1", @event = "BILL_PAID" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "NotConfigured",
            await response.Content.ReadAsStringAsync(CancellationToken.None),
            StringComparison.Ordinal);
    }

    // Rota sem tenant não existe: o webhook é POR CONTA desde o ADR-019 — e a resposta é 401,
    // não 404.
    //
    // `webhooks/asaas` sem o `{tenantId:guid}` não casa endpoint nenhum, e desde a fase 8 da
    // auditoria o fallback de autorização exige autenticação. O middleware o aplica TAMBÉM quando
    // nenhum endpoint casou, então a requisição para na porta antes de virar 404.
    //
    // O 401 é o desfecho melhor e por isso a expectativa mudou em vez da produção: ele não
    // confirma se a rota existe. Trocar o comportamento do servidor para devolver 404 seria abrir
    // um oráculo de rotas em troca de um teste.
    [Fact]
    public async Task Webhook_WithoutATenantInTheRoute_ShouldRespondUnauthorized()
    {
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync(
            new Uri("/webhooks/asaas", UriKind.Relative),
            new { id = "evt_1", @event = "BILL_PAID" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Token errado num tenant que TEM webhook é 401 — validado em tempo constante, antes de
    // olhar o corpo. É o único não-2xx que sobrou, e represar a fila de quem tenta forjar evento
    // de pagamento é exatamente o desfecho desejado.
    [Fact]
    public async Task Webhook_WithTheWrongToken_ShouldRespond401()
    {
        await LinkPaymentAccountAsync();

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

    // A REGRESSÃO DE 2026-09-09: o comprovante FALHA e o boleto espelha assim mesmo.
    //
    // O OutboxProcessor despacha TODOS os handlers de um evento dentro de UMA transação, e a
    // captura do comprovante roda depois do espelho. Enquanto ela lançava BLP.PMO21, a transação
    // inteira revertia — levando junto o boleto que já tinha virado Paid — e cinco tentativas
    // depois a mensagem ia para dead-letter. Desfecho: ordem Paid, boleto parado em Scheduled,
    // em silêncio, com o dinheiro fora da conta. Estado de boleto não depende de um PDF.
    [Fact]
    public async Task Webhook_Paid_WhenTheReceiptFetchFails_ShouldStillMirrorTheBill()
    {
        var (billId, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));

        _receipts.Scripted = ReceiptFetchResult.Unavailable("http_503");

        var response = await PostWebhookAsync(new
        {
            id = "evt_paid_no_receipt",
            @event = "BILL_PAID",
            bill = new { externalReference = orderId.ToString() },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await DrainOutboxAsync();

        Assert.Equal(PaymentOrderStatus.Paid, (await LoadOrderAsync(orderId)).Status);
        Assert.Equal(BillStatus.Paid, (await LoadBillAsync(billId)).Status);

        // O comprovante fica pendente — e é a varredura de comprovante que o persegue, com
        // backoff INFINITO, ao contrário do outbox.
        var order = await LoadOrderAsync(orderId);
        Assert.True(string.IsNullOrEmpty(order.ReceiptStorageKey));
        Assert.False(order.ReceiptUnavailable);

        // E a mensagem do outbox foi processada: nada de dead-letter por causa de um PDF.
        var deadLetters = await ExecuteDbContextAsync(db => db.OutboxDeadLetters.AsNoTracking().CountAsync());
        Assert.Equal(0, deadLetters);
    }

    // A idempotência por id de evento: a reentrega do mesmo evento não produz efeito nenhum.
    [Fact]
    public async Task Webhook_Redelivered_ShouldHaveNoSecondEffect()
    {
        var (_, orderId) = await SubmitOrderAsync();
        ScriptProviderStatus(PaymentOrderStatus.BankProcessing, "BANK_PROCESSING");

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

    // Regressão do webhook venenoso: BILL_PAID SEM paymentDate é payload incoerente (BLP.PMO03),
    // e a resposta é 200 com a marca do ledger persistida e a ordem intacta — devolver não-2xx
    // sem a marca faria o provedor reentregar o mesmo evento para sempre, represando a fila
    // sequencial de webhooks da conta inteira.
    [Fact]
    public async Task Webhook_PaidWithoutAPaymentDate_ShouldAcknowledgeWithoutPoisoningRedelivery()
    {
        var (_, orderId) = await SubmitOrderAsync();

        // A incoerência vem da RELEITURA, não do corpo do evento (ADR-019): o payload é aviso, e
        // o que decide é o que o provedor responde quando a ordem é relida.
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, PaidAt: null, null, [], null));

        var payload = new
        {
            id = "evt_poison_1",
            @event = "BILL_PAID",
            bill = new { externalReference = orderId.ToString() },
        };

        var first = await PostWebhookAsync(payload);
        var second = await PostWebhookAsync(payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("Incoherent", await first.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("Duplicate", await second.Content.ReadAsStringAsync(CancellationToken.None), StringComparison.Ordinal);

        var ledgerRows = await ExecuteDbContextAsync(db => db.PaymentWebhookEvents.AsNoTracking().CountAsync());
        Assert.Equal(1, ledgerRows);
        Assert.Equal(PaymentOrderStatus.Pending, (await LoadOrderAsync(orderId)).Status);
    }

    // Referência que não é nossa devolve 200: falhar faria o provedor reentregar para sempre um
    // evento de outra conta.
    //
    // A conta precisa estar vinculada: desde o ADR-022 o tenant SEM webhook responde 200
    // `NotConfigured` antes de olhar a referência, e sem isto o teste media o desfecho errado.
    [Fact]
    public async Task Webhook_WithAnUnknownReference_ShouldAcknowledgeWithoutEffect()
    {
        await LinkPaymentAccountAsync();

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
        ScriptProviderStatus(PaymentOrderStatus.BankProcessing, "BANK_PROCESSING");

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
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/webhooks/asaas/{TenantId}", UriKind.Relative))
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
        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        // O caminho do outbox NÃO grava a marca definitiva: o comprovante pode só não existir
        // AINDA — quem encerra a busca é a rede de segurança, na segunda olhada.
        Assert.False(order.ReceiptUnavailable);

        var receipt = await _client.GetAsync(
            new Uri($"/api/v1/{TenantId}/payments/{orderId}/receipt", UriKind.Relative), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, receipt.StatusCode);
    }

    // A rede de segurança (Definitive): sem URL no provedor, a marca persiste e tira a ordem da
    // varredura — sem ela, a conciliação perguntaria a mesma coisa para sempre.
    [Fact]
    public async Task CaptureReceipt_Definitive_WithoutAUrl_ShouldMarkTheOrderOutOfTheSweep()
    {
        var (_, orderId) = await SubmitOrderAsync();
        await MarkPaidAsync(orderId);

        var outcome = await CaptureReceiptAsync(orderId, definitive: true);

        Assert.Equal("NoReceipt", outcome);
        Assert.True((await LoadOrderAsync(orderId)).ReceiptUnavailable);

        using var scope = _host.Services.CreateScope();
        var workQueries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();
        var swept = await workQueries.ClaimPaidMissingReceiptAsync(
            DateTimeOffset.UtcNow.AddMinutes(1), 10, CancellationToken.None);

        Assert.DoesNotContain(swept, pending => pending.PaymentOrderId == orderId);
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

    // A REPESCAGEM DE 2026-09-09: ordem cujo comprovante ficou gravado como a PÁGINA do
    // provedor (.html) volta para a captura, recebe o PDF, e a página antiga sai do balde.
    // Sem isto, o acervo capturado antes do conserto continuaria abrindo a tela vazia — e com
    // um blob órfão a mais por ordem repescada.
    [Fact]
    public async Task CaptureReceipt_WhenTheStoredReceiptIsTheProvidersPage_ShouldReplaceItWithThePdf()
    {
        var (_, orderId) = await SubmitOrderAsync();
        await MarkPaidAsync(orderId);
        ScriptReceiptUrl();

        var storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();

        // O estado que o adapter antigo produzia: a página guardada como se fosse o comprovante.
        _receipts.Scripted = ReceiptFetchResult.Fetched("<html>comprovante</html>"u8.ToArray(), "text/html");
        Assert.Equal("Stored", await CaptureReceiptAsync(orderId));

        var landingPageKey = (await LoadOrderAsync(orderId)).ReceiptStorageKey;
        Assert.EndsWith(".html", landingPageKey, StringComparison.Ordinal);

        // O adapter consertado passa a entregar o PDF que a página oferecia.
        _receipts.Scripted = ReceiptFetchResult.Fetched(FakeReceiptFetcher.DefaultReceipt, "application/pdf");
        Assert.Equal("Stored", await CaptureReceiptAsync(orderId));

        var pdfKey = (await LoadOrderAsync(orderId)).ReceiptStorageKey;
        Assert.EndsWith(".pdf", pdfKey, StringComparison.Ordinal);
        Assert.False(storage.Contains(landingPageKey!), "a página antiga deveria ter saído do balde");

        // E é o PDF que chega ao app — o content-type é o que a tela usa para decidir renderizar.
        var receipt = await _client.GetAsync(
            new Uri($"/api/v1/{TenantId}/payments/{orderId}/receipt", UriKind.Relative), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        Assert.Equal("application/pdf", receipt.Content.Headers.ContentType?.MediaType);
    }

    // Repescagem que não melhorou nada não regrava: o provedor devolveu a página de novo, e
    // guardar outra cópia a cada ciclo da varredura só encheria o balde.
    [Fact]
    public async Task CaptureReceipt_WhenTheProviderStillServesThePage_ShouldKeepTheStoredCopy()
    {
        var (_, orderId) = await SubmitOrderAsync();
        await MarkPaidAsync(orderId);
        ScriptReceiptUrl();

        var storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
        _receipts.Scripted = ReceiptFetchResult.Fetched("<html>comprovante</html>"u8.ToArray(), "text/html");

        Assert.Equal("Stored", await CaptureReceiptAsync(orderId));
        var storedKey = (await LoadOrderAsync(orderId)).ReceiptStorageKey;
        var blobs = storage.Count;

        Assert.Equal("AlreadyStored", await CaptureReceiptAsync(orderId));

        Assert.Equal(blobs, storage.Count);
        Assert.Equal(storedKey, (await LoadOrderAsync(orderId)).ReceiptStorageKey);
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

        // O vínculo só ENFILEIRA o provisionamento; quem o executa é o outbox. Sem esta drenagem
        // o tenant fica sem webhook e todo evento entrante responde 200 NotConfigured — que é
        // exatamente o estado em que a instalação ficou por um mês, sem ninguém notar.
        await DrainOutboxAsync();
    }

    private async Task<HttpResponseMessage> PostWebhookAsync<T>(T payload, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/webhooks/asaas/{TenantId}", UriKind.Relative))
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

        await queries.ClaimPendingSubmissionsAsync(
            10, DateTimeOffset.UtcNow.AddMinutes(15), DateOnly.FromDateTime(DateTime.UtcNow),
            submissionWindowOpen: true, CancellationToken.None);
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
    /// <summary>
    /// O retrato que a RELEITURA vai encontrar no provedor.
    /// </summary>
    /// <remarks>
    /// O ADR-019 fez o payload do webhook virar aviso: o handler relê a ordem no provedor e
    /// aplica o que ELE responder. Teste que só manda o evento no corpo e não arma isto encontra
    /// o retrato padrão e vê a ordem parada — foi o que deixou três testes vermelhos desde
    /// 227383b8.
    /// </remarks>
    private void ScriptProviderStatus(PaymentOrderStatus status, string rawStatus)
        => _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", status, rawStatus,
            null, null, null, [], null));

    /// <summary>
    /// O provedor oferecendo o comprovante — sem URL o comando toma o caminho de "sem
    /// comprovante" e nunca chega ao fetcher.
    /// </summary>
    private void ScriptReceiptUrl()
        => _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [],
            "https://www.asaas.com/comprovantes/000123"));

    private async Task MarkPaidAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IPaymentOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = await orders.GetAsync(
            Domain.SharedKernel.TenantId.From(TenantId), PaymentOrderId.From(orderId), CancellationToken.None);

        order!.ApplyProviderStatus(
            PaymentOrderStatus.Paid, DateOnly.FromDateTime(DateTime.UtcNow), fee: null, null,
            DateTimeOffset.UtcNow, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(CancellationToken.None);
    }

    private async Task<string> CaptureReceiptAsync(Guid orderId, bool definitive = false)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(
            new CapturePaymentReceiptCommand(TenantId, orderId, definitive), CancellationToken.None);
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
