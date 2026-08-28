namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fila que o processador de artefatos consome.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureItemWorkQueriesTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly CaptureSourceId Source = CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    public CaptureItemWorkQueriesTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Só o que ainda não foi processado entra na fila — o resto já teve seu desfecho.
    [Fact]
    public async Task ListPending_ShouldReturnOnlyReceivedItems()
    {
        await SeedAsync("pendente.pdf", OccurredAt);
        await SeedAsync("ja-processado.pdf", OccurredAt, item =>
        {
            item.StoreArtifact("sha256:a", "chave", OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
        });

        var pending = await ListAsync(10);

        Assert.Single(pending);
    }

    // Ordem por chegada, porque boleto tem vencimento: o que entrou primeiro está mais perto de
    // vencer, e deixá-lo atrás numa fila cheia é como se perde prazo.
    [Fact]
    public async Task ListPending_ShouldReturnOldestFirst()
    {
        var recente = await SeedAsync("recente.pdf", OccurredAt);
        var antigo = await SeedAsync("antigo.pdf", OccurredAt.AddDays(-3));

        var pending = await ListAsync(10);

        Assert.Equal(antigo.Value, pending[0].CaptureItemId);
        Assert.Equal(recente.Value, pending[1].CaptureItemId);
    }

    // A fila atravessa tenants — o worker roda fora de requisição —, e cada item carrega o
    // tenant que reconstitui o escopo do comando seguinte.
    [Fact]
    public async Task ListPending_ShouldSpanTenantsAndCarryTheirScope()
    {
        await SeedAsync("do-tenant-a.pdf", OccurredAt, tenantId: TenantA);
        await SeedAsync("do-tenant-b.pdf", OccurredAt, tenantId: TenantB);

        var pending = await ListAsync(10);

        Assert.Equal(2, pending.Count);
        Assert.Contains(pending, p => p.TenantId == TenantA.Value);
        Assert.Contains(pending, p => p.TenantId == TenantB.Value);
    }

    // O teto de lote é respeitado: um ciclo não puxa a fila inteira para a memória.
    [Fact]
    public async Task ListPending_ShouldRespectTheLimit()
    {
        for (var i = 0; i < 5; i++)
            await SeedAsync($"anexo-{i}.pdf", OccurredAt.AddMinutes(i));

        Assert.Equal(2, (await ListAsync(2)).Count);
    }

    // Item cujo download falhou NÃO volta sozinho: insistir para sempre contra um anexo que o
    // provedor não entrega seria um laço sem fim, e a nova tentativa é decisão de quem opera.
    [Fact]
    public async Task ListPending_ShouldNotIncludeItemsWhoseDownloadFailed()
    {
        await SeedAsync("falhou.pdf", OccurredAt, item => item.MarkLinkFailed("artifact_download_failed", OccurredAt));

        Assert.Empty(await ListAsync(10));
    }

    // TESTE DE REGRESSÃO (2026-08-26): reivindicar tira o item da fila para os outros workers.
    // Sem isto, dois ciclos concorrentes pegavam o mesmo artefato e o processavam em duplicidade
    // — origem dos BLP.CPI03 de 'Promoted -> Parsed' observados em produção.
    [Fact]
    public async Task ClaimPending_ShouldNotHandTheSameItemTwice()
    {
        await SeedAsync("disputado.pdf", OccurredAt);

        var primeiro = await ListAsync(10);
        var segundo = await ListAsync(10);

        Assert.Single(primeiro);
        Assert.Empty(segundo);
    }

    // O aluguel vence sozinho: um worker que morre no meio não segura o artefato para sempre.
    // É o que substitui o faxineiro que outras filas precisam ter.
    [Fact]
    public async Task ClaimPending_WhenTheLeaseHasExpired_ShouldHandTheItemAgain()
    {
        await SeedAsync("abandonado.pdf", OccurredAt);

        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));

        Assert.Single(await ListAsync(10));
    }

    // A reivindicação conta a tentativa na saída da fila, e é esse contador que limita o laço.
    [Fact]
    public async Task ClaimPending_ShouldCountTheAttempt()
    {
        var id = await SeedAsync("contado.pdf", OccurredAt);

        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));
        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));

        var item = await ExecuteDbContextAsync(db => db.CaptureItems.FindAsync(id).AsTask());
        Assert.Equal(2, item!.ProcessingAttempts);
    }

    /// <remarks>
    /// <c>await</c> aqui não é estilo: devolver a <c>Task</c> sem esperar faria o <c>using</c>
    /// descartar o escopo — e a conexão — antes de a consulta terminar.
    /// </remarks>
    private Task<IReadOnlyList<PendingCaptureItem>> ListAsync(int limit)
        => ClaimAsync(limit, DateTimeOffset.UtcNow.AddMinutes(5));

    private async Task<IReadOnlyList<PendingCaptureItem>> ClaimAsync(int limit, DateTimeOffset leaseUntil)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        return await queries.ClaimPendingAsync(limit, leaseUntil);
    }

    private Task<CaptureItemId> SeedAsync(
        string artifactKey,
        DateTime receivedAt,
        Action<CaptureItem>? arrange = null,
        TenantId? tenantId = null)
        => ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                tenantId ?? TenantA,
                Source,
                $"msg-{artifactKey}",
                artifactKey,
                "faturas@fornecedor.com.br",
                "Assunto",
                receivedAt,
                receivedAt);

            arrange?.Invoke(item);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
}
