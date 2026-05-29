namespace EconomicCore.Infra.Outbox;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IOutboxProcessor
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
    Task<int> CleanupAsync(CancellationToken cancellationToken);
}

internal sealed class OutboxProcessor : IOutboxProcessor
{
    private const int MAX_ERROR_LENGTH = 4000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxEventTypeResolver _resolver;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOutboxEventTypeResolver resolver,
        TimeProvider timeProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _resolver = resolver;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var handled = 0;
        for (var i = 0; i < _options.BatchSize; i++)
        {
            var outcome = await ProcessOneAsync(cancellationToken);
            if (outcome == ProcessOutcome.Empty)
                break;
            handled++;
        }

        return handled;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);
        return await db.OutboxMessages
            .Where(m => m.Processed && m.ProcessedAt != null && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<ProcessOutcome> ProcessOneAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();

        var claimedId = Guid.Empty;
        var claimed = false;

        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                claimed = false;
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                var message = await ClaimNextAsync(db, _options.MaxAttempts, cancellationToken);
                if (message is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                claimed = true;
                claimedId = message.Id;

                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                var domainEvent = _resolver.Resolve(message);
                await dispatcher.DispatchAsync(domainEvent, cancellationToken);

                message.MarkProcessed(_timeProvider.GetUtcNow().UtcDateTime);
                await db.SaveEntitiesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });

            return claimed ? ProcessOutcome.Processed : ProcessOutcome.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The claim/dispatch transaction rolled back. If we had claimed a message, record the
            // failure out-of-band (separate transaction) so the attempt count survives.
            if (!claimed)
                throw;

            db.ChangeTracker.Clear();
            await RegisterFailureAsync(claimedId, ex, cancellationToken);
            return ProcessOutcome.Failed;
        }
    }

    private static async Task<OutboxMessage?> ClaimNextAsync(EconomicCoreDbContext db, int maxAttempts, CancellationToken cancellationToken)
    {
        // FOR UPDATE SKIP LOCKED lets multiple relay instances claim disjoint rows without blocking.
        // DEFAULT_SCHEMA is a compile-time constant; maxAttempts is parameterized ({0}) to avoid injection.
        var sql = $"SELECT * FROM {EconomicCoreDbContext.DEFAULT_SCHEMA}.outbox_messages "
                + "WHERE processed = false AND attempts < {0} ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED";

        var rows = await db.OutboxMessages.FromSqlRaw(sql, maxAttempts).ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    private async Task RegisterFailureAsync(Guid messageId, Exception failure, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconomicCoreDbContext>();

        var error = Truncate(failure.ToString(), MAX_ERROR_LENGTH);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var message = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
            if (message is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            message.RegisterFailure(error);

            if (message.Attempts >= _options.MaxAttempts)
            {
                db.OutboxDeadLetters.Add(OutboxDeadLetter.From(message, now));
                db.OutboxMessages.Remove(message);
                _logger.LogWarning(
                    "Outbox message {MessageId} ({EventType}) moved to dead-letter after {Attempts} attempts.",
                    message.Id, message.EventType, message.Attempts);
            }
            else
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} ({EventType}) failed on attempt {Attempts}; will retry.",
                    message.Id, message.EventType, message.Attempts);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private enum ProcessOutcome
    {
        Empty,
        Processed,
        Failed,
    }
}
