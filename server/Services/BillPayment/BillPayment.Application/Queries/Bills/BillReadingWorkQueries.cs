namespace BillPayment.Application.Queries.Bills;

using System.Data.Common;
using BillPayment.Domain.Bills;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Query side (CQRS) — exceção autorizada de dependência: toca a Infra direto, sem mediator.
/// </summary>
internal sealed class BillReadingWorkQueries(BillPaymentDbContext context, TimeProvider clock)
    : IBillReadingWorkQueries
{
    /// <summary>
    /// Escolhe e reserva o lote num único comando, para que ninguém escolha o que já foi escolhido.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É a mesma reivindicação atômica da fila de captura</strong>, e pelos mesmos três
    /// motivos: <c>FOR UPDATE SKIP LOCKED</c> faz cada worker trancar o que pegou e os demais
    /// pularem em vez de esperar; o <c>UPDATE</c> envolve o <c>SELECT</c> porque o aluguel precisa
    /// sobreviver à transação (a análise leva segundos e chama serviço externo, e segurar
    /// transação por todo esse tempo prenderia conexão à toa); e a condição do aluguel é também o
    /// backoff, porque depois de uma falha passageira o agregado empurra
    /// <c>reading_lease_expires_at</c> para o futuro e este mesmo <c>WHERE</c> passa a pular o
    /// boleto até lá.
    /// </para>
    /// <para>
    /// <strong>Mas aqui NÃO se pode usar <c>FromSqlRaw</c>, e a fila de captura pode.</strong> A
    /// diferença é o mapeamento: <c>Bill</c> tem <c>OwnsMany(e =&gt; e.Checks)</c> em tabela
    /// separada, e coleção owned é <em>auto-incluída</em> — o EF compõe um <c>SELECT</c> em volta
    /// de qualquer <c>FromSql</c> sobre <c>Bills</c> para trazer os filhos, e <c>UPDATE …
    /// RETURNING</c> não é composable. O resultado media-se em log: <em>todo</em> ciclo morria em
    /// <c>InvalidOperationException</c> antes de reivindicar coisa alguma, e o boleto ficava em
    /// "Na fila para análise" para sempre. <c>CaptureItem</c> não tem coleção owned, e por isso a
    /// fila de lá sobrevive ao mesmo código.
    /// </para>
    /// <para>
    /// <strong>Por isso o comando é ADO direto.</strong> Não há tipo de entidade envolvido, então
    /// não há o que compor: o que volta são dois <c>uuid</c> por linha, que é exatamente o que o
    /// worker precisa. Materializar o agregado seria trabalho jogado fora — os agregados não
    /// atravessam escopo, e quem lê de verdade é o <c>ApplyBillReadingCommand</c>, no escopo dele.
    /// </para>
    /// <para>
    /// <strong>O mais antigo primeiro.</strong> A cota de IA é escassa, e gastá-la com o que
    /// chegou agora deixaria o de ontem esperando indefinidamente. A ordem do <c>RETURNING</c> não
    /// é especificada pelo Postgres, então a ordenação final é refeita aqui.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<PendingBillReading>> ClaimPendingReadingsAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;
        var now = clock.GetUtcNow().UtcDateTime;

        // Só o nome do schema é interpolado, e ele é constante de compilação; todo valor vindo de
        // fora entra parametrizado.
        var sql =
            $"UPDATE {schema}.bills SET "
            + "reading_lease_expires_at = @lease, reading_attempts = reading_attempts + 1, updated_at = @now "
            + "WHERE id IN ("
            + $"SELECT id FROM {schema}.bills "
            + "WHERE reading_state = @state "
            + "AND (reading_lease_expires_at IS NULL OR reading_lease_expires_at <= @now) "
            + "ORDER BY created_at, id LIMIT @limit FOR UPDATE SKIP LOCKED) "
            + "RETURNING id, tenant_id, created_at";

        var claimed = new List<(Guid Id, Guid TenantId, DateTime CreatedAt)>();

        // O EF conta as aberturas, então abrir aqui não fecha uma conexão que já estava em uso —
        // e a transação corrente, quando existe, precisa ser propagada à mão: um DbCommand cru
        // fora dela seria recusado pelo provedor.
        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            Bind(command, "@lease", leaseUntil.UtcDateTime);
            Bind(command, "@now", now);
            Bind(command, "@state", ReadingStatus.Queued.Id);
            Bind(command, "@limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                claimed.Add((
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetDateTime(2)));
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        return [.. claimed
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Select(b => new PendingBillReading(b.TenantId, b.Id))];
    }

    /// <summary>
    /// O tipo é INFERIDO, e para data isso depende do <c>Kind</c> — não o declare à mão.
    /// </summary>
    /// <remarks>
    /// As colunas de aluguel são <c>timestamp with time zone</c>, e o Npgsql escolhe entre
    /// <c>timestamptz</c> e <c>timestamp</c> pelo <c>DateTimeKind</c> do valor: <c>Utc</c> resolve
    /// para o primeiro. Declarar <c>DbType.DateTime2</c> forçaria o segundo e a comparação sairia
    /// errada. Todo valor que chega aqui vem de <c>UtcDateTime</c>, então o <c>Kind</c> é <c>Utc</c>
    /// por construção.
    /// </remarks>
    private static void Bind(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }
}
