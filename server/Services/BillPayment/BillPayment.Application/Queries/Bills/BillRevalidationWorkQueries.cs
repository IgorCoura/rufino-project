namespace BillPayment.Application.Queries.Bills;

using System.Data.Common;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
internal sealed class BillRevalidationWorkQueries(BillPaymentDbContext context, TimeProvider clock)
    : IBillRevalidationWorkQueries
{
    /// <summary>
    /// Escolhe, conta a tentativa e adia a próxima — tudo num comando só.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>ADO direto pelo mesmo motivo da fila de leitura</strong>, e não por gosto:
    /// <c>Bill</c> mapeia <c>Checks</c> com <c>OwnsMany</c> em tabela separada, coleção owned é
    /// auto-incluída, e o EF compõe um <c>SELECT</c> em volta de qualquer <c>FromSql</c> sobre
    /// <c>Bills</c> — <c>UPDATE … RETURNING</c> não é composable. Aqui não há tipo de entidade
    /// envolvido, então não há o que compor.
    /// </para>
    /// <para>
    /// <strong>Quem entra é quem a consulta oficial não alcançou por indisponibilidade</strong>, e
    /// só isso. <c>lookup_unresolved</c> fica de fora porque o provedor já respondeu que não
    /// conhece o título — reconsultar devolve o mesmo e martelaria o provedor à toa;
    /// <c>lookup_not_configured</c> fica de fora porque não depende do tempo, e sim de o tenant
    /// vincular a conta (ver <see cref="ReleaseNotConfiguredAsync"/>).
    /// </para>
    /// <para>
    /// <strong>E só boleto que ainda aceita revalidação silenciosa.</strong> Revalidar um
    /// <c>Approved</c> deixou de derrubar a aprovação incondicionalmente (2026-09-10), mas
    /// <strong>este</strong> caso é justamente o que a derruba: a fila existe para boleto cujo
    /// <c>LookupAvailability</c> reprovou, e quando a consulta volta a responder o desfecho DAQUELE
    /// check muda — então a aprovação cairia, em silêncio, por obra de um worker. A regra não
    /// mudou; o motivo dela ficou mais preciso. Efeito colateral bom: aprovar assumindo o risco
    /// tira o boleto da fila sozinho.
    /// </para>
    /// <para>
    /// <strong>A tentativa é contada na SAÍDA</strong>, junto com o adiamento. Contar só as falhas
    /// registradas deixaria de fora a pior delas — a que derruba o worker antes de escrever
    /// qualquer coisa —, e o boleto voltaria sem espera nenhuma.
    /// </para>
    /// <para>
    /// <strong><c>updated_at</c> NÃO é tocado.</strong> Ele significa "mudou de negócio", e uma
    /// varredura que roda de cinco em cinco minutos o tornaria inútil justamente durante uma queda
    /// longa. É a mesma separação que o <c>LastSweptAt</c> da varredura de expectativas ensinou.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<PendingBillRevalidation>> ClaimStaleWithoutLookupAsync(
        int limit,
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;
        var now = clock.GetUtcNow().UtcDateTime;

        // Só o nome do schema é interpolado, e ele é constante de compilação; todo valor vindo de
        // fora entra parametrizado. O backoff é calculado no próprio UPDATE porque depende do
        // contador DAQUELA linha — e no SET o Postgres lê o valor anterior, que é o que se quer.
        var sql =
            $"UPDATE {schema}.bills SET "
            + "revalidation_attempts = revalidation_attempts + 1, "
            + "revalidation_next_attempt_at = @now + make_interval("
            + "secs => LEAST(@maxSeconds, @baseSeconds * POWER(2, revalidation_attempts))) "
            + "WHERE id IN ("
            + $"SELECT b.id FROM {schema}.bills b "
            + $"JOIN {schema}.bill_checks c ON c.bill_id = b.id "
            + "WHERE b.status IN (@captured, @awaiting) "
            + "AND c.type = @checkType AND c.reason_code = @reason "
            + "AND (b.revalidation_next_attempt_at IS NULL OR b.revalidation_next_attempt_at <= @now) "
            + "ORDER BY b.revalidation_next_attempt_at NULLS FIRST, b.created_at, b.id "
            + "LIMIT @limit FOR UPDATE OF b SKIP LOCKED) "
            + "RETURNING id, tenant_id, revalidation_attempts, created_at";

        var claimed = new List<(Guid Id, Guid TenantId, int Attempts, DateTime CreatedAt)>();

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            Bind(command, "@now", now);
            Bind(command, "@baseSeconds", baseDelay.TotalSeconds);
            Bind(command, "@maxSeconds", maxDelay.TotalSeconds);
            Bind(command, "@captured", BillStatus.Captured.Id);
            Bind(command, "@awaiting", BillStatus.AwaitingApproval.Id);
            Bind(command, "@checkType", CheckType.LookupAvailability.Id);
            Bind(command, "@reason", CheckReasons.LOOKUP_UNAVAILABLE);
            Bind(command, "@limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                claimed.Add((
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3)));
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        // A ordem do RETURNING não é especificada pelo Postgres, então a do lote é refeita aqui:
        // o que espera há mais tempo vai primeiro.
        return [.. claimed
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Select(b => new PendingBillRevalidation(b.TenantId, b.Id, b.Attempts))];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Zera o backoff em vez de revalidar aqui: quem revalida é a fila de sempre, no escopo dela.
    /// Um segundo caminho de revalidação seria um segundo lugar para as regras envelhecerem.
    /// </remarks>
    public async Task<int> ReleaseNotConfiguredAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;

        var sql =
            $"UPDATE {schema}.bills SET revalidation_attempts = 0, revalidation_next_attempt_at = NULL "
            + "WHERE tenant_id = @tenant AND status IN (@captured, @awaiting) AND id IN ("
            + $"SELECT c.bill_id FROM {schema}.bill_checks c "
            + "WHERE c.type = @checkType AND c.reason_code = @reason)";

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            Bind(command, "@tenant", tenantId);
            Bind(command, "@captured", BillStatus.Captured.Id);
            Bind(command, "@awaiting", BillStatus.AwaitingApproval.Id);
            Bind(command, "@checkType", CheckType.LookupAvailability.Id);
            Bind(command, "@reason", CheckReasons.LOOKUP_NOT_CONFIGURED);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// O tipo é INFERIDO — ver a nota do <c>BillReadingWorkQueries</c>: declarar
    /// <c>DbType.DateTime2</c> à mão faria a coluna <c>timestamptz</c> ser comparada como
    /// <c>timestamp</c>. Todo valor de data que chega aqui vem de <c>UtcDateTime</c>.
    /// </summary>
    private static void Bind(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}
