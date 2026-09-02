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

    private static void Bind(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}
