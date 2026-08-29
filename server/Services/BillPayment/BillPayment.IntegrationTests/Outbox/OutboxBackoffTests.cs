namespace BillPayment.IntegrationTests.Outbox;

using BillPayment.Infra.Outbox;
using BillPayment.Infra.Persistence;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O relay espera antes de tentar de novo — e a espera está na linha, não na memória.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class OutboxBackoffTests : BaseIntegrationTest
{
    public OutboxBackoffTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Regressão (auditoria 2026-08-28): a mensagem que falhou era reivindicada de novo no ciclo
    // seguinte, e cinco ciclos de 5s esgotavam as tentativas em 25 segundos de provedor fora.
    // Agora a falha grava next_attempt_at no futuro, e a próxima passagem NÃO a pega.
    [Fact]
    public async Task ProcessPending_AfterAFailure_ShouldWaitForTheBackoffBeforeRetrying()
    {
        var id = Guid.CreateVersion7();
        // Tipo de evento que não existe: o resolvedor falha, e a falha é registrada com backoff.
        await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO {BillPaymentDbContext.DEFAULT_SCHEMA}.outbox_messages "
            + "(id, event_type, payload, occurred_at, created_at, processed, attempts) "
            + "VALUES ({0}, 'BillPayment.Domain.Nao.Existe', '{{}}', now(), now(), false, 0)",
            id));

        using var scope = Factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        var first = await processor.ProcessPendingAsync(CancellationToken.None);
        var second = await processor.ProcessPendingAsync(CancellationToken.None);

        var message = await ExecuteDbContextAsync(db => db.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == id));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, message.Attempts);
        Assert.NotNull(message.NextAttemptAt);
        Assert.True(message.NextAttemptAt > DateTime.UtcNow.AddSeconds(20));
        Assert.False(message.Processed);
    }
}
