namespace BillPayment.IntegrationTests.CapturedMessages;

using BillPayment.Application.Queries.CapturedMessages;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O desfecho que a linha da tela mostra sem expandir — e o que ele NÃO pode dizer.
/// </summary>
/// <remarks>
/// <para>
/// Teste de regressão de 2026-08-26: vários e-mails apareciam eternamente como <c>Na fila</c>.
/// Não havia fila nenhuma — o backend tinha zero anexos pendentes. Eram mensagens **sem anexo**
/// (propaganda, notificação, cobrança sem documento), que entram no livro-caixa para quem mandou
/// o e-mail ter resposta, mas não produzem anexo nem item. O cálculo do desfecho dominante
/// percorria os anexos, não achava nenhum, e caía no fallback <c>Pending</c> — que a tela traduz
/// como "Na fila". Medido na caixa real: <strong>23 de 39 mensagens</strong>.
/// </para>
/// <para>
/// O segundo defeito era irmão: <c>ProcessingFailed</c> foi acrescentado ao catálogo e esquecido
/// na lista de prioridade, então o anexo que desistiu de processar também escorria para
/// <c>Pending</c> e aparecia como se ainda estivesse esperando.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CapturedMessageOutcomeTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly CaptureSourceId Source =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e2"));

    private static readonly DateTime OccurredAt = new(2026, 8, 26, 20, 0, 0, DateTimeKind.Utc);

    public CapturedMessageOutcomeTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // TESTE ÂNCORA: mensagem sem anexo não está esperando nada, e a tela não pode dizer que está.
    [Fact]
    public async Task ListAsync_WhenTheMessageHasNoArtifacts_ShouldNotReportItAsPending()
    {
        await SeedWithoutArtifactsAsync("72x num seminovo que vale a pena");

        var page = await ListAsync();

        var message = Assert.Single(page.Items);
        Assert.Equal(nameof(ArtifactOutcome.NothingToProcess), message.Outcome);
        Assert.NotEqual(nameof(ArtifactOutcome.Pending), message.Outcome);
    }

    // TESTE DE EROSÃO: todo desfecho do catálogo tem de aparecer na tela como ele mesmo.
    // Um valor esquecido na lista de prioridade escorre para `Pending` — sem quebrar compilação
    // nem teste nenhum —, e o usuário passa a ler "Na fila" sobre um anexo já decidido. Foi
    // assim que `ProcessingFailed` sumiu no dia em que nasceu.
    [Theory]
    [InlineData(nameof(ArtifactOutcome.Promoted))]
    [InlineData(nameof(ArtifactOutcome.Unrouted))]
    [InlineData(nameof(ArtifactOutcome.ForeignPayer))]
    [InlineData(nameof(ArtifactOutcome.Quarantined))]
    [InlineData(nameof(ArtifactOutcome.Locked))]
    [InlineData(nameof(ArtifactOutcome.DownloadFailed))]
    [InlineData(nameof(ArtifactOutcome.Discarded))]
    [InlineData(nameof(ArtifactOutcome.ProcessingFailed))]
    [InlineData(nameof(ArtifactOutcome.Dismissed))]
    public async Task ListAsync_ShouldSurfaceEveryOutcomeAsItself(string outcomeName)
    {
        var outcome = Enumeration.GetAll<ArtifactOutcome>()
            .Single(o => string.Equals(o.Name, outcomeName, StringComparison.Ordinal));

        await SeedWithArtifactAsync($"anexo-{outcomeName}", outcome);

        var page = await ListAsync();

        Assert.Equal(outcomeName, Assert.Single(page.Items).Outcome);
    }

    // A contraprova do teste acima: o catálogo não cresceu sem que os InlineData acompanhassem.
    // Sem ela, acrescentar um desfecho e esquecer o caso deixaria a bateria verde e cega.
    [Fact]
    public void EveryOutcomeIsCoveredByTheErosionTheory()
    {
        var cobertos = new[]
        {
            ArtifactOutcome.Promoted, ArtifactOutcome.Unrouted, ArtifactOutcome.ForeignPayer,
            ArtifactOutcome.Quarantined, ArtifactOutcome.Locked, ArtifactOutcome.DownloadFailed,
            ArtifactOutcome.Discarded, ArtifactOutcome.ProcessingFailed, ArtifactOutcome.Dismissed,

            // Os dois que a teoria não exercita, por não serem desfecho de anexo decidido:
            // `Pending` é trânsito, e `NothingToProcess` é a ausência de anexo (teste âncora).
            ArtifactOutcome.Pending, ArtifactOutcome.NothingToProcess,
        };

        Assert.Equal(Enumeration.GetAll<ArtifactOutcome>().Count(), cobertos.Distinct().Count());
    }

    // Filtrar por "nada a processar" devolve justamente as mensagens sem anexo — casar pelo
    // `Any` sobre anexos devolveria lista vazia, que é o oposto do que o filtro promete.
    [Fact]
    public async Task ListAsync_FilteringByNothingToProcess_ShouldReturnTheMessagesWithoutArtifacts()
    {
        await SeedWithoutArtifactsAsync("propaganda");
        await SeedWithArtifactAsync("boleto.pdf", ArtifactOutcome.Promoted);

        var page = await ListAsync(outcome: nameof(ArtifactOutcome.NothingToProcess));

        Assert.Equal("propaganda", Assert.Single(page.Items).Subject);
    }

    // E o filtro dos desfechos de verdade continua ignorando as mensagens sem anexo.
    [Fact]
    public async Task ListAsync_FilteringByARealOutcome_ShouldNotReturnMessagesWithoutArtifacts()
    {
        await SeedWithoutArtifactsAsync("propaganda");
        await SeedWithArtifactAsync("boleto.pdf", ArtifactOutcome.Promoted);

        var page = await ListAsync(outcome: nameof(ArtifactOutcome.Promoted));

        Assert.Equal("boleto.pdf", Assert.Single(page.Items).Subject);
    }

    private async Task<CapturedMessagePage> ListAsync(string? outcome = null)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICapturedMessageQueries>();

        return await queries.ListAsync(
            Tenant.Value, outcome, sourceId: null, from: null, to: null,
            search: null, cursor: null, limit: 50);
    }

    private Task SeedWithoutArtifactsAsync(string subject)
        => ExecuteDbContextAsync(async db =>
        {
            var captured = CapturedMessage.Register(
                Tenant, Source, $"AAMkAGI2-{subject}", "marketing@loja.com.br", subject,
                OccurredAt, OccurredAt, artifacts: []);

            await db.CapturedMessages.AddAsync(captured);
            await db.SaveEntitiesAsync();
        });

    private Task SeedWithArtifactAsync(string subject, ArtifactOutcome outcome)
        => ExecuteDbContextAsync(async db =>
        {
            var captured = CapturedMessage.Register(
                Tenant, Source, $"AAMkAGI2-{subject}", "faturas@enel.com.br", subject,
                OccurredAt, OccurredAt, [("anexo.pdf", "anexo.pdf", "application/pdf")]);

            captured.RecordOutcome(
                "anexo.pdf", outcome, reason: null, captureItemId: null, billId: null, OccurredAt);

            await db.CapturedMessages.AddAsync(captured);
            await db.SaveEntitiesAsync();
        });
}
