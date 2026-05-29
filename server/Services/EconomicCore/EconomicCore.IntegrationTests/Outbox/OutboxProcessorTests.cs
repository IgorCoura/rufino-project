namespace EconomicCore.IntegrationTests.Outbox;

using System.Net.Http.Json;
using EconomicCore.Infra.Outbox;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(nameof(IntegrationTestCollection))]
public sealed class OutboxProcessorTests : BaseIntegrationTest
{
    public OutboxProcessorTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private IOutboxProcessor Processor => Factory.Services.GetRequiredService<IOutboxProcessor>();

    // Processar o outbox após registrar um recurso despacha o evento ao handler (projeção gravada) e marca a mensagem como processada.
    [Fact]
    public async Task ProcessPending_DispatchesHandler_AndMarksProcessed()
    {
        SetRequestId();
        var resp = await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Imovel Outbox Pipeline", "SERVICE"));
        resp.EnsureSuccessStatusCode();

        var handled = await Processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, handled);

        var message = await ExecuteDbContextAsync(db => db.OutboxMessages.AsNoTracking().SingleAsync());
        Assert.True(message.Processed);
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(0, message.Attempts);

        var projection = await ExecuteDbContextAsync(db => db.ProcessedEventLogs.AsNoTracking().SingleAsync());
        Assert.Equal("Imovel Outbox Pipeline", projection.Name);
        Assert.Equal(message.OccurredAt, projection.OccurredAt);
    }

    // Rodar o processamento duas vezes não reaplica o efeito: a segunda passada não encontra mensagem pendente.
    [Fact]
    public async Task ProcessPending_IsIdempotentAcrossRuns()
    {
        SetRequestId();
        await Client.PostAsJsonAsync(
            $"/api/v1/{KnownIds.TenantA}/resources",
            new CreateResourceRequest("Imovel Once", "SERVICE"));

        await Processor.ProcessPendingAsync(CancellationToken.None);
        var second = await Processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(0, second);
        var count = await ExecuteDbContextAsync(db => db.ProcessedEventLogs.AsNoTracking().CountAsync());
        Assert.Equal(1, count);
    }

    // Mensagem com tipo de evento desconhecido falha repetidamente e, ao estourar MaxAttempts, é movida para a dead-letter.
    [Fact]
    public async Task ProcessPending_WhenEventTypeUnknown_MovesToDeadLetterAfterMaxAttempts()
    {
        var poisonId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await ExecuteDbContextAsync(async db =>
            await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO economic_core.outbox_messages (id, event_type, payload, occurred_at, created_at, processed, attempts)
VALUES ({poisonId}, {"Nonexistent.Event.Type"}, {"{}"}, {now}, {now}, false, 0)"));

        await Processor.ProcessPendingAsync(CancellationToken.None);

        var stillQueued = await ExecuteDbContextAsync(db =>
            db.OutboxMessages.AsNoTracking().AnyAsync(m => m.Id == poisonId));
        Assert.False(stillQueued);

        var deadLetter = await ExecuteDbContextAsync(db =>
            db.OutboxDeadLetters.AsNoTracking().SingleAsync(d => d.Id == poisonId));
        Assert.Equal(5, deadLetter.Attempts);
        Assert.Contains("Nonexistent.Event.Type", deadLetter.Error);
    }

    // Cleanup remove mensagens processadas além da janela de retenção e preserva as recentes.
    [Fact]
    public async Task Cleanup_RemovesProcessedMessagesPastRetention()
    {
        var oldId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await ExecuteDbContextAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO economic_core.outbox_messages (id, event_type, payload, occurred_at, created_at, processed, processed_at, attempts)
VALUES ({oldId}, {"X"}, {"{}"}, {now.AddDays(-30)}, {now.AddDays(-30)}, true, {now.AddDays(-30)}, 0)");
            await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO economic_core.outbox_messages (id, event_type, payload, occurred_at, created_at, processed, processed_at, attempts)
VALUES ({recentId}, {"X"}, {"{}"}, {now}, {now}, true, {now}, 0)");
        });

        var deleted = await Processor.CleanupAsync(CancellationToken.None);

        Assert.Equal(1, deleted);
        var remaining = await ExecuteDbContextAsync(db =>
            db.OutboxMessages.AsNoTracking().Select(m => m.Id).ToListAsync());
        Assert.DoesNotContain(oldId, remaining);
        Assert.Contains(recentId, remaining);
    }

    private void SetRequestId()
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", Guid.NewGuid().ToString());
    }
}
