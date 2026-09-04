namespace BillPayment.IntegrationTests.CapturedMessages;

using BillPayment.Application.Queries.CapturedMessages;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A ordem da tela de e-mails capturados: do recebimento mais recente para o mais antigo.
/// </summary>
/// <remarks>
/// <para>
/// É a ordem em que alguém procura "o e-mail que acabei de mandar", e por isso ela é contrato da
/// tela, não detalhe da consulta.
/// </para>
/// <para>
/// O empate de <c>ReceivedAt</c> não é borda inventada: uma varredura traz mensagens que
/// chegaram no mesmo segundo, e o provedor carimba o recebimento com precisão de segundo. Com o
/// cursor carregando só a data, a página 2 filtraria <c>ReceivedAt &lt; T</c> e pularia os
/// empatados — sem erro e sem log, com a lista afirmando que acabou. Foi exatamente o bug de
/// 2026-08-11 do <c>CursorCodec</c>, aqui na chave nova.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CapturedMessageOrderingTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly CaptureSourceId Source =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e1"));

    private static readonly DateTime OccurredAt = new(2026, 8, 19, 20, 0, 0, DateTimeKind.Utc);

    public CapturedMessageOrderingTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // O contrato da tela: o mais recente primeiro.
    [Fact]
    public async Task ListAsync_ShouldOrderByReceivedAtDescending()
    {
        await SeedAsync(
            ("mais-antigo", new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc)),
            ("mais-recente", new DateTime(2026, 8, 19, 15, 56, 0, DateTimeKind.Utc)),
            ("do-meio", new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc)));

        var page = await ListAsync(cursor: null, limit: 50);

        Assert.Equal(
            new List<string?> { "mais-recente", "do-meio", "mais-antigo" },
            page.Items.Select(m => m.Subject).ToList());
    }

    // A ordem tem que atravessar as páginas: paginar não pode embaralhar.
    [Fact]
    public async Task ListAsync_ShouldKeepTheOrderAcrossPages()
    {
        await SeedAsync(
            ("d1", new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc)),
            ("d2", new DateTime(2026, 8, 16, 9, 0, 0, DateTimeKind.Utc)),
            ("d3", new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc)),
            ("d4", new DateTime(2026, 8, 18, 9, 0, 0, DateTimeKind.Utc)),
            ("d5", new DateTime(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc)));

        var seen = new List<DateTime>();
        string? cursor = null;

        do
        {
            var page = await ListAsync(cursor, limit: 2);
            seen.AddRange(page.Items.Select(m => m.ReceivedAt));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(5, seen.Count);
        Assert.Equal(seen.OrderByDescending(d => d).ToList(), seen);
    }

    // Teste de regressão da lição do CursorCodec: com o recebimento empatado, a página seguinte
    // não pode voltar vazia nem pular ninguém.
    [Fact]
    public async Task ListAsync_WhenReceivedAtTies_ShouldWalkEveryPageWithoutRepeating()
    {
        var sameInstant = new DateTime(2026, 8, 19, 15, 56, 0, DateTimeKind.Utc);

        await SeedAsync(
            ("a", sameInstant),
            ("b", sameInstant),
            ("c", sameInstant),
            ("d", sameInstant),
            ("e", sameInstant));

        var seen = new List<Guid>();
        string? cursor = null;

        do
        {
            var page = await ListAsync(cursor, limit: 2);
            seen.AddRange(page.Items.Select(m => m.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct().Count());
    }

    // O filtro não pode desmanchar a ordem — nem repetir linha entre páginas.
    [Fact]
    public async Task ListAsync_WithAnOutcomeFilter_ShouldStayOrderedAndNotRepeat()
    {
        await SeedAsync(
            ("d1", new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc)),
            ("d2", new DateTime(2026, 8, 16, 9, 0, 0, DateTimeKind.Utc)),
            ("d3", new DateTime(2026, 8, 17, 9, 0, 0, DateTimeKind.Utc)));

        var seen = new List<DateTime>();
        string? cursor = null;

        do
        {
            var page = await ListAsync(cursor, limit: 2, outcome: nameof(ArtifactOutcome.Discarded));
            seen.AddRange(page.Items.Select(m => m.ReceivedAt));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(3, seen.Count);
        Assert.Equal(seen.OrderByDescending(d => d).ToList(), seen);
    }

    private async Task<CapturedMessagePage> ListAsync(string? cursor, int limit, string? outcome = null)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICapturedMessageQueries>();

        return await queries.ListAsync(
            Tenant.Value, outcome, sourceId: null, from: null, to: null,
            search: null, cursor: cursor, limit: limit);
    }

    private Task SeedAsync(params (string Subject, DateTime ReceivedAt)[] messages)
        => ExecuteDbContextAsync(async db =>
        {
            var index = 0;

            foreach (var (subject, receivedAt) in messages)
            {
                var captured = CapturedMessage.Register(
                    Tenant,
                    Source,
                    $"AAMkAGI2THVSAAA={index++}",
                    "faturas@enel.com.br",
                    subject,
                    receivedAt,
                    OccurredAt,
                    [("anexo.pdf", "anexo.pdf", "application/pdf")]);

                captured.RecordOutcome(
                    "anexo.pdf", ArtifactOutcome.Discarded, "no_instrument_in_document",
                    captureItemId: null, billId: null, OccurredAt);

                await db.CapturedMessages.AddAsync(captured);
            }

            await db.SaveEntitiesAsync();
        });
}
