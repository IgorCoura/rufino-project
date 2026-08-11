namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Paginação da lista de itens capturados quando <c>CreatedAt</c> empata.
/// </summary>
/// <remarks>
/// O empate aqui não é caso de borda inventado pelo teste: uma varredura de caixa carimba um
/// instante só e o repassa a <strong>todos</strong> os itens que ingere. Medido em produção em
/// 2026-08-11: 404 itens, <c>count(DISTINCT created_at) = 1</c>.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureItemPaginationTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));
    private static readonly CaptureSourceId Source = CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c2"));

    /// <summary>O mesmo instante para todos, que é o que a varredura faz.</summary>
    private static readonly DateTime SameInstant = new(2026, 8, 11, 18, 14, 25, DateTimeKind.Utc);

    public CaptureItemPaginationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Teste de regressão. Bug de 2026-08-11: o cursor carregava só o CreatedAt, então a página 2
    // filtrava CreatedAt > T e voltava VAZIA — com 404 itens no banco carimbados no mesmo
    // instante, tudo além da primeira página ficava inalcançável, sem erro e sem log, com a
    // lista afirmando que havia acabado.
    [Fact]
    public async Task ListAsync_WhenEveryItemSharesTheSameCreatedAt_ShouldWalkEveryPage()
    {
        var seeded = await SeedAsync(5);

        var seen = new List<Guid>();
        string? cursor = null;

        do
        {
            var page = await ListAsync(cursor, limit: 2);
            seen.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(seeded.Count, seen.Count);
        Assert.Equal(seeded.Count, seen.Distinct().Count());
        Assert.Equal(seeded.OrderBy(id => id).ToList(), seen.OrderBy(id => id).ToList());
    }

    // A página nunca repete item: o desempate por Id avança mesmo com todo mundo no mesmo
    // instante, em vez de reapresentar a mesma fatia.
    [Fact]
    public async Task ListAsync_WhenPagingThroughATie_ShouldNotRepeatItems()
    {
        await SeedAsync(4);

        var first = await ListAsync(cursor: null, limit: 2);
        var second = await ListAsync(first.NextCursor, limit: 2);

        Assert.Equal(2, first.Items.Count);
        Assert.Equal(2, second.Items.Count);
        Assert.Empty(first.Items.Select(i => i.Id).Intersect(second.Items.Select(i => i.Id)));
    }

    // Cursor no formato antigo (8 bytes, só a data) reinicia a lista em vez de estourar ou
    // devolver página pela metade — mesma degradação de um cursor corrompido ou forjado.
    [Fact]
    public async Task ListAsync_WithALegacyDateOnlyCursor_ShouldRestartFromTheBeginning()
    {
        await SeedAsync(3);

        var legacy = Convert.ToBase64String(BitConverter.GetBytes(SameInstant.Ticks));

        var page = await ListAsync(legacy, limit: 10);

        Assert.Equal(3, page.Items.Count);
    }

    private async Task<IReadOnlyList<Guid>> SeedAsync(int count)
        => await ExecuteDbContextAsync(async db =>
        {
            var ids = new List<Guid>(count);

            for (var i = 0; i < count; i++)
            {
                var item = CaptureItem.Ingest(
                    Tenant,
                    Source,
                    $"msg-{i}",
                    $"anexo-{i}.pdf",
                    "faturas@fornecedor.com.br",
                    "Assunto",
                    SameInstant,
                    SameInstant);

                await db.CaptureItems.AddAsync(item);
                ids.Add(item.Id.Value);
            }

            await db.SaveEntitiesAsync();
            return (IReadOnlyList<Guid>)ids;
        });

    /// <remarks>
    /// <c>await</c> aqui não é estilo: devolver a <c>Task</c> sem esperar faria o <c>using</c>
    /// descartar o escopo — e a conexão — antes de a consulta terminar.
    /// </remarks>
    private async Task<CaptureItemPage> ListAsync(string? cursor, int limit)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemQueries>();

        return await queries.ListAsync(Tenant.Value, status: null, cursor, limit);
    }
}
