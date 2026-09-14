namespace BillPayment.IntegrationTests.CaptureItems;

using System.Text;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O corpo do e-mail e o anexo da MESMA mensagem descrevem, quase sempre, o mesmo boleto.
/// </summary>
/// <remarks>
/// Bug de 2026-09-14: a fatura da Vivo traz o Pix e o código de barras escritos no corpo e o PDF
/// anexado. Processados em paralelo, viravam dois itens independentes, e o do corpo — sem documento
/// fiscal nenhum — caía na fila de reivindicação com o HTML do e-mail no lugar do documento.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class MessageBodySiblingTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime OccurredAt = new(2026, 9, 13, 3, 20, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";
    private const string MessageId = "AAMkAGI2VIVOAAA=";
    private const string Sender = "contadigitalvivo@vivo.com.br";

    private const string OtherPixPayload =
        "00020101021226770014BR.GOV.BCB.PIX2555api.itau/pix/qr/v2/fe80130d-c5ef-407b-94a5-f6b2005020095"
        + "204000053039865802BR5906SABESP6009SAO PAULO62070503***6304A76E";

    private readonly IServiceProvider _services;

    public MessageBodySiblingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
    }

    // O corpo não sai da fila enquanto o anexo da mesma mensagem ainda não foi processado — e o
    // anexo sai normalmente.
    [Fact]
    public async Task Claim_WhileAnAttachmentOfTheSameMessageIsPending_ShouldHoldTheBody()
    {
        var sourceId = await SeedSourceAsync();
        var body = await SeedItemAsync(sourceId, IMailboxReader.BODY_ARTIFACT_KEY, "text/html", []);
        var attachment = await SeedItemAsync(sourceId, "AAMkANEXO01", "application/pdf", []);

        var claimed = await ClaimAsync();

        Assert.Contains(claimed, c => c.CaptureItemId == attachment.Value);
        Assert.DoesNotContain(claimed, c => c.CaptureItemId == body.Value);
    }

    // Decidido o anexo, o corpo volta a ser entregue.
    [Fact]
    public async Task Claim_WhenTheAttachmentHasBeenDecided_ShouldHandTheBody()
    {
        var sourceId = await SeedSourceAsync();
        var body = await SeedItemAsync(sourceId, IMailboxReader.BODY_ARTIFACT_KEY, "text/html", []);
        await SeedItemAsync(sourceId, "AAMkANEXO01", "application/pdf", [],
            item => item.MarkLinkFailed("artifact_download_failed", OccurredAt));

        Assert.Contains(await ClaimAsync(), c => c.CaptureItemId == body.Value);
    }

    // A CONTRAPROVA: corpo de mensagem sem anexo nenhum não espera nada. Sem ela, uma consulta que
    // segurasse todo corpo passaria nos dois testes acima.
    [Fact]
    public async Task Claim_WhenTheMessageHasNoAttachment_ShouldHandTheBody()
    {
        var sourceId = await SeedSourceAsync();
        var body = await SeedItemAsync(sourceId, IMailboxReader.BODY_ARTIFACT_KEY, "text/html", []);

        Assert.Contains(await ClaimAsync(), c => c.CaptureItemId == body.Value);
    }

    // TESTE DE REGRESSÃO. O anexo já resolveu o boleto, e o corpo repete a mesma linha digitável:
    // o corpo é descartado como duplicata do anexo, em vez de ir para a reivindicação com o HTML.
    [Fact]
    public async Task Process_WhenTheBodyRepeatsTheAttachmentsBoleto_ShouldDiscardTheBodyAsItsDuplicate()
    {
        var sourceId = await SeedSourceAsync();
        var attachment = await SeedItemAsync(sourceId, "AAMkANEXO01", "application/pdf", PdfWith(ValidBankSlip));
        var body = await SeedItemAsync(sourceId, IMailboxReader.BODY_ARTIFACT_KEY, "text/html",
            Html($"<td>Código de barras</td><td>{ValidBankSlip}</td>"));

        await ProcessAsync(attachment);
        var result = await ProcessAsync(body);

        Assert.Equal("Discarded", result.Decision);

        var stored = await LoadAsync(body);
        Assert.Same(CaptureItemStatus.Discarded, stored!.Status);
        Assert.Equal(attachment, stored.DiscardedOf);
        Assert.False(stored.HasStoredArtifact);
    }

    // A CONTRAPROVA: corpo com OUTRO boleto que o do anexo continua sendo um boleto próprio.
    [Fact]
    public async Task Process_WhenTheBodyCarriesADifferentBoleto_ShouldKeepItAsItsOwnItem()
    {
        var sourceId = await SeedSourceAsync();
        var attachment = await SeedItemAsync(sourceId, "AAMkANEXO01", "application/pdf", PdfWith(ValidBankSlip));
        var body = await SeedItemAsync(sourceId, IMailboxReader.BODY_ARTIFACT_KEY, "text/html",
            Html($"<td>PIX</td><td>{OtherPixPayload}</td>"));

        await ProcessAsync(attachment);
        var result = await ProcessAsync(body);

        Assert.Equal("Parse", result.Decision);
        Assert.NotSame(CaptureItemStatus.Discarded, (await LoadAsync(body))!.Status);
    }

    private async Task<IReadOnlyList<PendingCaptureItem>> ClaimAsync()
    {
        using var scope = _services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        return await queries.ClaimPendingAsync(10, DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: false));

        return first.Decision != "VisionPending"
            ? first
            : await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: true));
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

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

    private async Task<CaptureItemId> SeedItemAsync(
        CaptureSourceId sourceId,
        string artifactKey,
        string contentType,
        byte[] content,
        Action<CaptureItem>? arrange = null)
    {
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, MessageId, artifactKey, Sender, "Sua Fatura Digital Vivo Chegou",
                OccurredAt, OccurredAt, contentType, fileName: null);

            arrange?.Invoke(item);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }

    private static byte[] Html(string inner)
        => Encoding.UTF8.GetBytes($"<html><body><table><tr>{inner}</tr></table></body></html>");

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
}
