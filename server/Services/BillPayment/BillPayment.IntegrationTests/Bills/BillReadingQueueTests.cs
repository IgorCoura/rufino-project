namespace BillPayment.IntegrationTests.Bills;

using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fila da leitura por IA dos boletos — a reivindicação atômica que o worker consome.
/// </summary>
/// <remarks>
/// <strong>Esta classe existe por causa de um defeito que passou por todas as outras.</strong> A
/// fila nunca tinha sido exercitada por teste nenhum: o worker era o único caminho até ela, e ele
/// engole a exceção do ciclo em <c>LogError</c> para não morrer. O resultado foi uma fila que
/// falhava em 100% das execuções sem nenhum sinal fora do log — e boletos anunciando "Na fila
/// para consulta com IA" indefinidamente.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillReadingQueueTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly Guid Source = new("0195a1f0-0000-7000-8000-0000000000b1");
    private static readonly DateTime OccurredAt = new(2026, 8, 28, 9, 0, 0, DateTimeKind.Utc);

    // Instrumentos sintéticos com DVs corretos, um por boleto: a chave de dedup é ÚNICA GLOBAL
    // (ix_bills_dedup_key_active, sem tenant_id), então dois boletos da mesma linha não coexistem
    // — nem entre tenants diferentes. Reusar uma linha aqui reprovaria o teste no banco, não na
    // fila, e esconderia o que ele existe para medir.
    private static readonly string[] BankSlipLines =
    [
        "34191234546789012345767890123457314880000061507",
        "03399876534321098765743210987657414930000140980",
        "826600000010224812345672890123456786901234567898",
    ];

    public BillReadingQueueTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // TESTE DE REGRESSÃO (2026-08-28): reivindicar a fila de leitura por IA tem que DEVOLVER o
    // boleto enfileirado. A consulta montava `UPDATE ... RETURNING` com `FromSqlRaw` sobre
    // `Bills`, e como `Bill` mapeia `Checks` com `OwnsMany` em tabela separada — coleção owned é
    // auto-incluída —, o EF compunha um SELECT em volta do comando e estourava
    // `InvalidOperationException: non-composable SQL`. Todo ciclo do worker morria antes de
    // reivindicar coisa alguma, e nenhum boleto jamais saía de "Na fila".
    [Fact]
    public async Task ClaimPendingReadings_ShouldReturnTheQueuedBill()
    {
        var billId = await SeedQueuedAsync();

        var pending = await ClaimAsync(10);

        Assert.Single(pending);
        Assert.Equal(billId.Value, pending[0].BillId);
        Assert.Equal(TenantA.Value, pending[0].TenantId);
    }

    // Só quem espera pela IA entra: boleto que já tem retrato, ou que não tem documento para ler,
    // não pode ocupar vaga de um lote cuja cota é escassa.
    [Fact]
    public async Task ClaimPendingReadings_ShouldReturnOnlyQueuedBills()
    {
        await SeedQueuedAsync();
        await SeedAsync(
            bill => bill.RecordReadingFailure(permanent: true, maxAttempts: 0, TimeSpan.Zero, OccurredAt),
            line: 1);

        var pending = await ClaimAsync(10);

        Assert.Single(pending);
    }

    // A fila atravessa tenants — o worker roda fora de requisição —, e cada linha carrega o
    // tenant que reconstitui o escopo do comando seguinte.
    [Fact]
    public async Task ClaimPendingReadings_ShouldSpanTenantsAndCarryTheirScope()
    {
        await SeedQueuedAsync(TenantA);
        await SeedQueuedAsync(TenantB, line: 1);

        var pending = await ClaimAsync(10);

        Assert.Equal(2, pending.Count);
        Assert.Contains(pending, p => p.TenantId == TenantA.Value);
        Assert.Contains(pending, p => p.TenantId == TenantB.Value);
    }

    // Reivindicar tira o boleto da fila para os outros workers, no mesmo comando que o escolheu.
    [Fact]
    public async Task ClaimPendingReadings_ShouldNotHandTheSameBillTwice()
    {
        await SeedQueuedAsync();

        var primeiro = await ClaimAsync(10);
        var segundo = await ClaimAsync(10);

        Assert.Single(primeiro);
        Assert.Empty(segundo);
    }

    // O aluguel vence sozinho: um worker que morre no meio da análise não segura o boleto para
    // sempre. É o que dispensa o faxineiro que filas assim costumam precisar.
    [Fact]
    public async Task ClaimPendingReadings_WhenTheLeaseHasExpired_ShouldHandTheBillAgain()
    {
        await SeedQueuedAsync();

        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));

        Assert.Single(await ClaimAsync(10));
    }

    // A tentativa é contada na SAÍDA da fila: um worker que morre antes de escrever qualquer
    // coisa não pode deixar o boleto voltando para sempre.
    [Fact]
    public async Task ClaimPendingReadings_ShouldCountTheAttempt()
    {
        var billId = await SeedQueuedAsync();

        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));
        await ClaimAsync(10, DateTimeOffset.UtcNow.AddSeconds(-30));

        var bill = await ExecuteDbContextAsync(db => db.Bills.FindAsync(billId).AsTask());
        Assert.Equal(2, bill!.ReadingAttempts);
    }

    // O mais antigo primeiro: a cota de IA é escassa, e gastá-la com o que chegou agora deixaria
    // o de ontem esperando indefinidamente.
    [Fact]
    public async Task ClaimPendingReadings_ShouldReturnOldestFirst()
    {
        var recente = await SeedQueuedAsync();
        var antigo = await SeedQueuedAsync(line: 1, createdAt: OccurredAt.AddDays(-3));

        var pending = await ClaimAsync(10);

        Assert.Equal(antigo.Value, pending[0].BillId);
        Assert.Equal(recente.Value, pending[1].BillId);
    }

    // O teto de lote é respeitado: um ciclo não puxa a fila inteira nem gasta a cota do dia.
    [Fact]
    public async Task ClaimPendingReadings_ShouldRespectTheLimit()
    {
        for (var i = 0; i < BankSlipLines.Length; i++)
            await SeedQueuedAsync(line: i, createdAt: OccurredAt.AddMinutes(i));

        Assert.Equal(2, (await ClaimAsync(2)).Count);
    }

    private Task<IReadOnlyList<PendingBillReading>> ClaimAsync(int limit)
        => ClaimAsync(limit, DateTimeOffset.UtcNow.AddMinutes(5));

    /// <remarks>
    /// <c>await</c> aqui não é estilo: devolver a <c>Task</c> sem esperar faria o <c>using</c>
    /// descartar o escopo — e a conexão — antes de a consulta terminar.
    /// </remarks>
    private async Task<IReadOnlyList<PendingBillReading>> ClaimAsync(int limit, DateTimeOffset leaseUntil)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillReadingWorkQueries>();

        return await queries.ClaimPendingReadingsAsync(limit, leaseUntil);
    }

    /// <summary>Boleto com documento guardado e sem retrato — o que nasce <c>Queued</c>.</summary>
    private Task<BillId> SeedQueuedAsync(
        TenantId? tenantId = null, int line = 0, DateTime? createdAt = null)
        => SeedAsync(arrange: null, line, tenantId, createdAt);

    private Task<BillId> SeedAsync(
        Action<Bill>? arrange,
        int line = 0,
        TenantId? tenantId = null,
        DateTime? createdAt = null)
        => ExecuteDbContextAsync(async db =>
        {
            var occurredAt = createdAt ?? OccurredAt;

            var bill = Bill.Capture(
                tenantId ?? TenantA,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(BankSlipLines[line], occurredAt))],
                BillOrigin.Create(
                    BillSourceKind.Mailbox,
                    occurredAt,
                    sourceId: Source,
                    senderAddress: "faturas@fornecedor.com.br",
                    storageKey: $"tenants/captures/{Guid.NewGuid()}"),
                occurredAt);

            arrange?.Invoke(bill);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });
}
