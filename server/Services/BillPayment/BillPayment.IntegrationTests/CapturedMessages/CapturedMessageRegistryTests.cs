namespace BillPayment.IntegrationTests.CapturedMessages;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Retention.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Retention;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O livro-caixa da captura, atravessando a cadeia real.
/// </summary>
/// <remarks>
/// O que estes testes protegem é a razão de o agregado existir: <strong>o item some no
/// descarte</strong> — decisão medida, porque 250 de 404 anexos de uma caixa real não tinham
/// sinal de cobrança e encheriam a quarentena —, e sem este registro ninguém consegue dizer o
/// que houve com um e-mail que mandou.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CapturedMessageRegistryTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(TestTenants.Primary);
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlip = "34191234546789012345767890123457314880000061507";
    private const string Sender = "faturas@fornecedor.com.br";
    private const string MessageId = "AAMkAGI2THVSAAA=";
    private const string InternetMessageId = "<abc123@fornecedor.com.br>";

    private readonly IServiceProvider _services;

    public CapturedMessageRegistryTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _services = factory.WithCaptureChain().Services;

    // O teste-âncora: o anexo é descartado, o item deixa de existir, e o registro continua lá
    // dizendo o que houve.
    [Fact]
    public async Task Process_WhenTheArtifactIsDiscarded_ShouldKeepTheMessageInTheRegistry()
    {
        var (itemId, artifactKey) = await SeedAsync(PdfWith("Contrato de locacao", "Clausula primeira"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Drop", result.Decision);
        Assert.Null(await LoadItemAsync(itemId));

        var message = await LoadMessageAsync();
        Assert.NotNull(message);

        var artifact = Assert.Single(message!.Artifacts);
        Assert.Equal(artifactKey, artifact.ArtifactKey);
        Assert.Equal(ArtifactOutcome.Discarded, artifact.Outcome);

        // Descartado não tem para onde navegar — o item foi apagado.
        Assert.Null(artifact.CaptureItemId);
        Assert.False(message.ProducedBill);
    }

    // O e-mail que virou boleto guarda o vínculo, e é o que a tela usa para levar até ele.
    [Fact]
    public async Task Process_WhenTheArtifactBecomesABill_ShouldLinkItInTheRegistry()
    {
        var (itemId, _) = await SeedAsync(PdfWith("Banco Itau", BankSlip));

        await ProcessAsync(itemId);

        var message = await LoadMessageAsync();
        var artifact = Assert.Single(message!.Artifacts);

        Assert.Equal(ArtifactOutcome.Unrouted, artifact.Outcome);
        Assert.Equal(itemId, artifact.CaptureItemId);
    }

    // Registro que produziu boleto não é purgado, esteja a janela em 7 ou em 180 dias: é trilha
    // de auditoria de um pagamento, e prazo de histórico operacional não a alcança.
    [Fact]
    public async Task Purge_ShouldNeverRemoveARecordThatProducedABill()
    {
        await SeedRetentionAsync(enabled: true, RetentionWindow.SevenDays);
        await SeedRegistryAsync(withBill: true, receivedAt: OccurredAt.AddYears(-1));

        var purged = await PurgeAsync();

        Assert.Equal(0, purged);
        Assert.NotNull(await LoadMessageAsync());
    }

    [Fact]
    public async Task Purge_ShouldRemoveAnExpiredRecordWithoutABill()
    {
        await SeedRetentionAsync(enabled: true, RetentionWindow.SevenDays);
        await SeedRegistryAsync(withBill: false, receivedAt: OccurredAt.AddYears(-1));

        var purged = await PurgeAsync();

        Assert.Equal(1, purged);
        Assert.Null(await LoadMessageAsync());
    }

    // Desligada é o estado inicial, e desligada não apaga nada — quem nunca abriu a tela não
    // perde histórico sem ter escolhido.
    [Fact]
    public async Task Purge_WhenThePolicyIsDisabled_ShouldRemoveNothing()
    {
        await SeedRetentionAsync(enabled: false, RetentionWindow.SevenDays);
        await SeedRegistryAsync(withBill: false, receivedAt: OccurredAt.AddYears(-1));

        var purged = await PurgeAsync();

        Assert.Equal(0, purged);
        Assert.NotNull(await LoadMessageAsync());
    }

    // Dentro da janela nada sai, mesmo com a política ligada.
    [Fact]
    public async Task Purge_WithinTheWindow_ShouldRemoveNothing()
    {
        await SeedRetentionAsync(enabled: true, RetentionWindow.OneHundredEightyDays);
        await SeedRegistryAsync(withBill: false, receivedAt: DateTime.UtcNow.AddDays(-3));

        var purged = await PurgeAsync();

        Assert.Equal(0, purged);
    }

    private static byte[] PdfWith(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var y = 800;
        foreach (var line in lines)
        {
            page.AddText(line, 10, new UglyToad.PdfPig.Core.PdfPoint(30, y), font);
            y -= 20;
        }

        return builder.Build();
    }

    // Reprovar também escreve no livro-caixa: um item reprovado cujo registro ainda dissesse
    // "aguardando" faria a tela de e-mails e a de quarentena discordarem sobre o mesmo documento.
    [Fact]
    public async Task Dismiss_ShouldRecordTheOutcomeInTheRegistry()
    {
        // Origem cadastrada para o item chegar à quarentena em vez de ser descartado — é de lá
        // que a reprovação parte.
        await SeedTrustedOriginAsync();

        var (itemId, artifactKey) = await SeedAsync(PdfWith("Boleto que nao reconheco"));
        await ProcessAsync(itemId);

        using (var scope = _services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new DismissCaptureItemCommand(
                Tenant.Value, itemId.Value, Guid.CreateVersion7(), "nao reconheco"));
        }

        var message = await LoadMessageAsync();
        var artifact = Assert.Single(message!.Artifacts);

        Assert.Equal(artifactKey, artifact.ArtifactKey);
        Assert.Equal(ArtifactOutcome.Dismissed, artifact.Outcome);
        Assert.Equal("nao reconheco", artifact.Reason);
    }

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // As DUAS passagens, como os dois workers fazem em produção: a faixa rápida cede a vez
        // quando precisa da IA, e a faixa lenta retoma. Rodar só a primeira descreveria metade
        // do fluxo — e foi o que estes testes faziam antes de as filas serem separadas.
        var first = await mediator.Send(
            new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: false));

        return first.Decision != "VisionPending"
            ? first
            : await mediator.Send(
                new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: true));
    }

    private async Task<int> PurgeAsync()
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PurgeCapturedMessagesCommand(Tenant.Value, 100));
        return result.Purged;
    }

    /// <summary>Cria fonte, registro e item, e programa o conteúdo que o download vai devolver.</summary>
    private async Task<(CaptureItemId ItemId, string ArtifactKey)> SeedAsync(byte[] content)
    {
        var sourceId = await SeedSourceAsync();
        var artifactKey = "anexo-boleto.pdf";

        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        var itemId = await ExecuteDbContextAsync(async db =>
        {
            var captured = CapturedMessage.Register(
                Tenant, sourceId, MessageId, Sender, "Assunto", OccurredAt, OccurredAt,
                [(artifactKey, "boleto.pdf", "application/pdf")], InternetMessageId);

            await db.CapturedMessages.AddAsync(captured);

            var item = CaptureItem.Ingest(
                Tenant, sourceId, MessageId, artifactKey, Sender, "Assunto", OccurredAt, OccurredAt,
                "application/pdf", "boleto.pdf", InternetMessageId);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

        return (itemId, artifactKey);
    }

    private Task SeedTrustedOriginAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var origin = TrustedOrigin.Register(
                Tenant,
                OriginKind.EmailAddress,
                Sender,
                TrustDecision.Trusted,
                UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1")),
                note: null,
                OccurredAt);

            await db.TrustedOrigins.AddAsync(origin);
            await db.SaveEntitiesAsync();
        });

    private Task<CaptureSourceId> SeedSourceAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(Guid.CreateVersion7()),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

    private Task SeedRetentionAsync(bool enabled, RetentionWindow window)
        => ExecuteDbContextAsync(async db =>
        {
            var policy = CaptureRetentionPolicy.Default(Tenant, OccurredAt);
            policy.Configure(enabled, window, OccurredAt);

            await db.CaptureRetentionPolicies.AddAsync(policy);
            await db.SaveEntitiesAsync();
        });

    private async Task SeedRegistryAsync(bool withBill, DateTime receivedAt)
    {
        var sourceId = await SeedSourceAsync();

        await ExecuteDbContextAsync(async db =>
        {
            var captured = CapturedMessage.Register(
                Tenant, sourceId, MessageId, Sender, "Assunto", receivedAt, OccurredAt,
                [("anexo.pdf", "anexo.pdf", "application/pdf")], InternetMessageId);

            captured.RecordOutcome(
                "anexo.pdf",
                withBill ? ArtifactOutcome.Promoted : ArtifactOutcome.Discarded,
                reason: null,
                captureItemId: null,
                billId: withBill ? BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d9")) : null,
                OccurredAt);

            await db.CapturedMessages.AddAsync(captured);
            await db.SaveEntitiesAsync();
        });
    }

    private Task<CaptureItem?> LoadItemAsync(CaptureItemId id)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id));

    private Task<CapturedMessage?> LoadMessageAsync()
        => ExecuteDbContextAsync(db => db.CapturedMessages
            .AsNoTracking()
            .Include(m => m.Artifacts)
            .FirstOrDefaultAsync(m => m.TenantId == Tenant));
}
