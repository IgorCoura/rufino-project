namespace BillPayment.IntegrationTests.Payments;

using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fila que o worker de submissão consome — a mesma semântica de reivindicação atômica da
/// fila de captura (<see cref="CaptureItems.CaptureItemWorkQueriesTests"/>), agora com dinheiro:
/// entregar a mesma ordem a dois workers é candidato a pagamento duplicado.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class PaymentOrderWorkQueriesTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(TestTenants.Primary);
    private static readonly TenantId TenantB = TenantId.From(TestTenants.Secondary);
    private static readonly DateTime OccurredAt = new(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);

    public PaymentOrderWorkQueriesTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Reivindicar tira a ordem da fila para os outros workers — com o aluguel vigente, a mesma
    // ordem nunca é entregue duas vezes (a lição dos BLP.CPI03 da fila de captura, aqui com dinheiro).
    [Fact]
    public async Task Claim_ShouldNotHandTheSameOrderTwice()
    {
        await SeedDraftAsync();

        var first = await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15));
        var second = await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.Single(first);
        Assert.Empty(second);
    }

    // O aluguel vence sozinho: um worker que morre no meio da submissão não segura a ordem para
    // sempre — a ordem volta à fila quando o prazo passa, sem faxineiro.
    [Fact]
    public async Task Claim_WhenTheLeaseHasExpired_ShouldReturnTheOrderToTheQueue()
    {
        await SeedDraftAsync();

        await ClaimAsync(DateTimeOffset.UtcNow.AddSeconds(-30));

        Assert.Single(await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    // Ordem retida (sem conta, ou aguardando confirmação do vencido) NUNCA é reivindicada —
    // retenção é estado visível fora da fila, não fila girando.
    [Fact]
    public async Task Claim_ShouldNeverHandHeldOrders()
    {
        await SeedDraftAsync(arrange: order => order.HoldForMissingAccount(OccurredAt));
        await SeedDraftAsync(arrange: order => order.HoldForConfirmation(OccurredAt));

        Assert.Empty(await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    // O backoff da falha passageira é o próprio aluguel: uma ordem cuja submissão falhou há
    // pouco fica fora da fila até o prazo — insistir na mesma hora bateria no mesmo provedor caído.
    [Fact]
    public async Task Claim_ShouldSkipOrdersWaitingOutTheirBackoff()
    {
        await SeedDraftAsync(arrange: order => order.RecordSubmissionFailure(
            permanent: false, "timeout", maxAttempts: 5, TimeSpan.FromMinutes(10), DateTime.UtcNow));

        Assert.Empty(await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    // A tentativa é contada na SAÍDA da fila — um worker que morre depois de submeter deixa a
    // contagem certa para a retentativa começar pela consulta de externalReference.
    [Fact]
    public async Task Claim_ShouldCountTheAttemptOnTheWayOut()
    {
        var orderId = await SeedDraftAsync();

        await ClaimAsync(DateTimeOffset.UtcNow.AddSeconds(-30));
        await ClaimAsync(DateTimeOffset.UtcNow.AddSeconds(-30));

        var order = await LoadOrderAsync(orderId);
        Assert.Equal(2, order.SubmissionAttempts);
    }

    // A fila atravessa tenants (o worker roda sem HttpContext) e cada linha carrega o tenant que
    // reconstitui o escopo do comando de submissão; a ordem é por chegada.
    [Fact]
    public async Task Claim_ShouldSpanTenantsOldestFirst()
    {
        var older = await SeedDraftAsync(tenantId: TenantB, createdAt: OccurredAt.AddMinutes(-10));
        var newer = await SeedDraftAsync(tenantId: TenantA, createdAt: OccurredAt);

        var claimed = await ClaimAsync(DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.Equal(2, claimed.Count);
        Assert.Equal(older.Value, claimed[0].PaymentOrderId);
        Assert.Equal(TenantB.Value, claimed[0].TenantId);
        Assert.Equal(newer.Value, claimed[1].PaymentOrderId);
    }

    // A varredura da conciliação: só ordem submetida (Pending/BankProcessing) e ENVELHECIDA
    // entra — a recém-submetida ainda vai receber webhook, e Draft nem tem o que conciliar.
    [Fact]
    public async Task ListStale_ShouldReturnOnlyAgedSubmittedOrders()
    {
        await SeedDraftAsync();
        var submitted = await SeedDraftAsync(arrange: order => order.MarkSubmitted(
            "pay_stale", new DateOnly(2026, 8, 21), null, null, DateTime.UtcNow));

        var stale = await ListStaleAsync(DateTimeOffset.UtcNow.AddMinutes(1));
        var fresh = await ListStaleAsync(DateTimeOffset.UtcNow.AddMinutes(-30));

        Assert.Equal(submitted.Value, Assert.Single(stale).PaymentOrderId);
        Assert.Empty(fresh);
    }

    // Ordem sincronizada há pouco é pulada — o carimbo LastProviderSyncAt é quem governa depois
    // da primeira notícia do provedor.
    [Fact]
    public async Task ListStale_ShouldSkipRecentlySyncedOrders()
    {
        await SeedDraftAsync(arrange: order =>
        {
            order.MarkSubmitted("pay_synced", new DateOnly(2026, 8, 21), null, null, DateTime.UtcNow);
            order.ApplyProviderStatus(
                PaymentOrderStatus.BankProcessing, null, null, null, DateTimeOffset.UtcNow, DateTime.UtcNow);
        });

        Assert.Empty(await ListStaleAsync(DateTimeOffset.UtcNow.AddMinutes(-5)));
        Assert.Single(await ListStaleAsync(DateTimeOffset.UtcNow.AddMinutes(1)));
    }

    private async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimAsync(DateTimeOffset leaseUntil)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        return await queries.ClaimPendingSubmissionsAsync(10, leaseUntil, CancellationToken.None);
    }

    private async Task<IReadOnlyList<PendingPaymentSubmission>> ListStaleAsync(DateTimeOffset syncedBefore)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();

        return await queries.ListStaleAwaitingProviderAsync(syncedBefore, 10, CancellationToken.None);
    }

    private Task<PaymentOrder> LoadOrderAsync(PaymentOrderId orderId)
        => ExecuteDbContextAsync(db => db.PaymentOrders
            .AsNoTracking()
            .SingleAsync(o => o.Id == orderId));

    private Task<PaymentOrderId> SeedDraftAsync(
        Action<PaymentOrder>? arrange = null,
        TenantId? tenantId = null,
        DateTime? createdAt = null)
        => ExecuteDbContextAsync(async db =>
        {
            var order = PaymentOrder.Draft(
                tenantId ?? TenantA,
                BillId.From(Guid.CreateVersion7()),
                PaymentRail.Boleto,
                new DateOnly(2026, 8, 25),
                new Money(615.07m, Currency.BRL),
                createdAt ?? OccurredAt);

            arrange?.Invoke(order);

            await db.PaymentOrders.AddAsync(order);
            await db.SaveEntitiesAsync();
            return order.Id;
        });
}
