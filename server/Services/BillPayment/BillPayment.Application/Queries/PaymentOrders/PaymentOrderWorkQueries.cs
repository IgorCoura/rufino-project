namespace BillPayment.Application.Queries.PaymentOrders;

using System.Data.Common;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
/// <remarks>
/// <strong>ADO direto, como a fila de leitura por IA</strong> — e aqui por dois motivos que se
/// somam: <c>PaymentOrder</c> tem token <c>xmin</c> (e <c>UPDATE … RETURNING *</c> não devolve
/// coluna de sistema, a armadilha registrada em gotchas), e materializar o agregado seria
/// trabalho jogado fora — ele não atravessa escopo, e quem o carrega é o
/// <c>SubmitPaymentOrderCommand</c>, no escopo dele.
/// </remarks>
internal sealed class PaymentOrderWorkQueries(BillPaymentDbContext context, TimeProvider clock)
    : IPaymentOrderWorkQueries
{
    public async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimPendingSubmissionsAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;
        var now = clock.GetUtcNow().UtcDateTime;

        // Só o nome do schema é interpolado, e ele é constante de compilação; todo valor de fora
        // entra parametrizado. A tentativa conta na SAÍDA da fila — um worker que morre depois de
        // submeter deixa a contagem certa para a retentativa começar pela consulta de referência.
        var sql =
            $"UPDATE {schema}.payment_orders SET "
            + "submission_lease_expires_at = @lease, submission_attempts = submission_attempts + 1, updated_at = @now "
            + "WHERE id IN ("
            + $"SELECT id FROM {schema}.payment_orders "
            + "WHERE status = @status AND hold = @hold "
            + "AND (submission_lease_expires_at IS NULL OR submission_lease_expires_at <= @now) "
            + "ORDER BY created_at, id LIMIT @limit FOR UPDATE SKIP LOCKED) "
            + "RETURNING id, tenant_id, created_at";

        var claimed = new List<(Guid Id, Guid TenantId, DateTime CreatedAt)>();

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            Bind(command, "@lease", leaseUntil.UtcDateTime);
            Bind(command, "@now", now);
            Bind(command, "@status", PaymentOrderStatus.Draft.Id);
            Bind(command, "@hold", PaymentOrderHold.None.Id);
            Bind(command, "@limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
                claimed.Add((reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2)));
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        return [.. claimed
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Select(o => new PendingPaymentSubmission(o.TenantId, o.Id))];
    }

    public async Task<IReadOnlyList<AccountHeldPaymentOrder>> ListAccountHeldAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var rows = await context.PaymentOrders
            .AsNoTracking()
            .Where(o => o.Status == PaymentOrderStatus.Draft && o.Hold == PaymentOrderHold.AwaitingAccount)
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Take(limit)
            .Select(o => new { o.TenantId, o.Id })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(o => new AccountHeldPaymentOrder(o.TenantId.Value, o.Id.Value))];
    }

    public async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimStaleAwaitingProviderAsync(
        DateTimeOffset syncedBefore,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        // Ordem sem sincronização nenhuma usa o updated_at como referência: acabou de ser
        // submetida e ainda não teve webhook — só entra quando também envelheceu. O carimbo
        // sweep_attempted_at na saída + NULLS FIRST é o anti-inanição: quem nunca foi tentada
        // passa na frente, quem falha repetidamente roda para o fim do lote.
        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;
        var sql =
            $"UPDATE {schema}.payment_orders SET sweep_attempted_at = @now "
            + "WHERE id IN ("
            + $"SELECT id FROM {schema}.payment_orders "
            + "WHERE status IN (@pending, @bank) "
            + "AND (CASE WHEN last_provider_sync_at IS NULL THEN updated_at ELSE last_provider_sync_at END) < @cutoff "
            + "ORDER BY sweep_attempted_at NULLS FIRST, last_provider_sync_at NULLS FIRST, id "
            + "LIMIT @limit FOR UPDATE SKIP LOCKED) "
            + "RETURNING id, tenant_id";

        return await ClaimSweepAsync(
            sql,
            syncedBefore.UtcDateTime,
            limit,
            command =>
            {
                Bind(command, "@pending", PaymentOrderStatus.Pending.Id);
                Bind(command, "@bank", PaymentOrderStatus.BankProcessing.Id);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimPaidMissingReceiptAsync(
        DateTimeOffset agedBefore,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        // Só ordem envelhecida (o caminho do outbox teve a vez dele) e sem a marca definitiva
        // de "sem comprovante" — a marca é o que impede a varredura eterna. O carimbo aqui e na
        // conciliação é a MESMA coluna, sem interferência: os status são disjuntos.
        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;
        var sql =
            $"UPDATE {schema}.payment_orders SET sweep_attempted_at = @now "
            + "WHERE id IN ("
            + $"SELECT id FROM {schema}.payment_orders "
            + "WHERE status IN (@paid, @refunded) "
            + "AND receipt_storage_key IS NULL AND receipt_unavailable = FALSE "
            + "AND provider_order_id IS NOT NULL "
            + "AND updated_at < @cutoff "
            + "AND (sweep_attempted_at IS NULL OR sweep_attempted_at < @cutoff) "
            + "ORDER BY sweep_attempted_at NULLS FIRST, updated_at, id "
            + "LIMIT @limit FOR UPDATE SKIP LOCKED) "
            + "RETURNING id, tenant_id";

        return await ClaimSweepAsync(
            sql,
            agedBefore.UtcDateTime,
            limit,
            command =>
            {
                Bind(command, "@paid", PaymentOrderStatus.Paid.Id);
                Bind(command, "@refunded", PaymentOrderStatus.Refunded.Id);
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<PendingPaymentSubmission>> ClaimSweepAsync(
        string sql,
        DateTime cutoff,
        int limit,
        Action<DbCommand> bindStatuses,
        CancellationToken cancellationToken)
    {
        var claimed = new List<(Guid Id, Guid TenantId)>();

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            Bind(command, "@now", clock.GetUtcNow().UtcDateTime);
            Bind(command, "@cutoff", cutoff);
            bindStatuses(command);
            Bind(command, "@limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
                claimed.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        return [.. claimed.Select(o => new PendingPaymentSubmission(o.TenantId, o.Id))];
    }

    private static void Bind(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}
