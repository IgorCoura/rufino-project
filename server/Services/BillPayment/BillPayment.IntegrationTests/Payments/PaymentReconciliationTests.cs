namespace BillPayment.IntegrationTests.Payments;

using System.Net.Http.Json;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Infra.Outbox;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A rede de segurança do webhook (UC-15): a conciliação consulta o provedor por uma ordem
/// parada e reflete o que ele sabe — webhook perdido não pode deixar ordem órfã.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class PaymentReconciliationTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid ApproverId = new("0195a1f0-0000-7000-8000-00000000000a");
    private static readonly DateTime ReceivedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;
    private readonly FakePaymentGateways _gateways;
    private readonly HttpClient _client;

    public PaymentReconciliationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithPaymentChain();
        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
        _gateways = _host.Services.GetRequiredService<FakePaymentGateways>();
        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _gateways.Reset();
        _client.Dispose();
        _host.Dispose();
    }

    // O caminho que substitui o webhook perdido: o provedor diz Paid, a conciliação aplica e o
    // boleto espelha — com exatamente UMA consulta de retrato.
    [Fact]
    public async Task Reconcile_ASubmittedOrder_ShouldApplyWhatTheProviderKnows()
    {
        var (billId, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Paid, "PAID",
            null, DateOnly.FromDateTime(DateTime.UtcNow), null, [], null));

        var outcome = await ReconcileAsync(orderId);

        Assert.Equal("Applied", outcome);
        Assert.Equal(1, _gateways.GetCalls);

        await DrainOutboxAsync();
        Assert.Equal(PaymentOrderStatus.Paid, (await LoadOrderAsync(orderId)).Status);
        Assert.Equal(BillStatus.Paid, (await LoadBillAsync(billId)).Status);
    }

    // O provedor não avançou: Unchanged, e a ordem ganha só o carimbo de sincronização — é ele
    // que a tira da frente da fila de conciliação.
    [Fact]
    public async Task Reconcile_WhenNothingChanged_ShouldReturnUnchangedAndStampTheSync()
    {
        var (_, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Found(new ProviderPaymentSnapshot(
            "pay_fake_1", PaymentOrderStatus.Pending, "PENDING", null, null, null, [], null));

        var outcome = await ReconcileAsync(orderId);

        Assert.Equal("Unchanged", outcome);
        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
        Assert.NotNull(order.LastProviderSyncAt);
    }

    // Provedor fora do ar: nada foi aprendido — nada muda, nem o carimbo, e a ordem continua
    // visível na fila para a próxima varredura.
    [Fact]
    public async Task Reconcile_WhenTheProviderIsUnavailable_ShouldChangeNothing()
    {
        var (_, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.Unavailable("timeout");

        var outcome = await ReconcileAsync(orderId);

        Assert.Equal("Unavailable", outcome);
        var order = await LoadOrderAsync(orderId);
        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
        Assert.Null(order.LastProviderSyncAt);
    }

    // O descompasso raro que exige gente: o provedor não conhece mais a própria ordem. Fica em
    // log e a ordem segue na fila — nunca vira desfecho por chute.
    [Fact]
    public async Task Reconcile_WhenTheProviderNoLongerKnowsTheOrder_ShouldLeaveItInTheQueue()
    {
        var (_, orderId) = await SubmitOrderAsync();
        _gateways.ScriptedGet = PaymentFetchResult.NotFound();

        var outcome = await ReconcileAsync(orderId);

        Assert.Equal("Unchanged", outcome);
        Assert.Equal(PaymentOrderStatus.Pending, (await LoadOrderAsync(orderId)).Status);
    }

    // Ordem que não espera desfecho do provedor (Draft, sem ProviderOrderId) é pulada SEM tocar
    // a rede — conciliar rascunho consultaria o provedor por algo que nunca foi submetido.
    [Fact]
    public async Task Reconcile_ADraftOrder_ShouldBeSkippedWithoutTouchingTheProvider()
    {
        var (_, orderId) = await SubmitOrderAsync(submit: false);

        var outcome = await ReconcileAsync(orderId);

        Assert.Equal("Skipped", outcome);
        Assert.Equal(0, _gateways.GetCalls);
    }

    // Ordem inexistente é Skipped — a conciliação é um job, e job não estoura por linha que sumiu.
    [Fact]
    public async Task Reconcile_AnUnknownOrder_ShouldBeSkipped()
    {
        Assert.Equal("Skipped", await ReconcileAsync(Guid.NewGuid()));
    }

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

    private static DateOnly ScheduleDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

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

    private async Task<HttpResponseMessage> PostBillAsync<T>(string path, T payload) where T : class
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri($"/api/v1/{TenantId}/bills/{path}", UriKind.Relative))
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add("x-user-id", ApproverId.ToString());
        request.Headers.Add("x-requestid", Guid.NewGuid().ToString());

        return await _client.SendAsync(request, CancellationToken.None);
    }

    private async Task<string> ReconcileAsync(Guid orderId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(
            new ReconcilePaymentOrderCommand(TenantId, orderId), CancellationToken.None);
        return response.Outcome;
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
