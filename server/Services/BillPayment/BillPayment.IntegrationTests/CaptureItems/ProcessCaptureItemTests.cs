namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// A cadeia completa da 2.3: baixar o artefato, extrair, triar e reter por desfecho.
/// </summary>
/// <remarks>
/// Prova a decisão do usuário (2026-08-11): o que não é boleto <strong>desaparece</strong>, e o
/// arquivo só é guardado quando há instrumento válido.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ProcessCaptureItemTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlip = "34191234546789012345767890123457314880000061507";
    private const string KnownSender = "faturas@fornecedor.com.br";
    private const string UnknownSender = "marketing@desconhecido.com";

    private readonly IServiceProvider _services;

    public ProcessCaptureItemTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _services = factory.WithCaptureChain().Services;

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

    // Artefato com boleto válido: vira Parsed e o arquivo É guardado — ele é o comprovante do
    // que o sistema viu quando decidiu pagar.
    [Fact]
    public async Task Process_WithValidBoleto_ShouldParseAndKeepTheArtifact()
    {
        var itemId = await SeedAsync(UnknownSender, PdfWith("Banco Itau", BankSlip));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.Equal(1, result.InstrumentsFound);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Parsed, stored!.Status);
        Assert.NotNull(stored.StorageKey);
        Assert.StartsWith("sha256:", stored.ContentHash!, StringComparison.Ordinal);
    }

    // Não é boleto e o remetente é desconhecido: o item DESAPARECE. É o que mantém a fila
    // utilizável — um balde cheio de e-mail irrelevante é um balde que ninguém olha.
    [Fact]
    public async Task Process_WithNonBoletoFromUnknownSender_ShouldDeleteTheItemEntirely()
    {
        var itemId = await SeedAsync(UnknownSender, PdfWith("Contrato de locacao", "Clausula primeira"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Drop", result.Decision);
        Assert.Null(await LoadAsync(itemId));
    }

    // Não é boleto MAS o remetente é cadastrado: fica na quarentena, porque a hipótese provável
    // é falha do parser, não ausência de boleto.
    [Fact]
    public async Task Process_WithNonBoletoFromKnownSender_ShouldQuarantineInsteadOfDropping()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, PdfWith("Fatura", "sem linha digitavel aqui"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrecognized, stored!.Status);
    }

    // Origem banida não ganha a exceção: o tenant já disse que não quer nada dali.
    [Fact]
    public async Task Process_WithNonBoletoFromBlockedSender_ShouldDrop()
    {
        await SeedTrustedOriginAsync(TrustDecision.Blocked);
        var itemId = await SeedAsync(KnownSender, PdfWith("Propaganda"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Drop", result.Decision);
        Assert.Null(await LoadAsync(itemId));
    }

    // Anexo que não veio NÃO é descartado: nada se aprendeu sobre ele, e a próxima tentativa
    // pode trazê-lo.
    [Fact]
    public async Task Process_WhenDownloadFails_ShouldKeepTheItemForRetry()
    {
        var itemId = await SeedAsync(UnknownSender, content: null);

        var result = await ProcessAsync(itemId);

        Assert.Equal("DownloadFailed", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.LinkFailed, stored!.Status);
    }

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value));
    }

    private Task SeedTrustedOriginAsync(TrustDecision? decision = null)
        => ExecuteDbContextAsync(async db =>
        {
            var origin = TrustedOrigin.Register(
                Tenant, OriginKind.EmailAddress, KnownSender, decision ?? TrustDecision.Trusted,
                Decider, note: null, OccurredAt);

            await db.TrustedOrigins.AddAsync(origin);
            await db.SaveEntitiesAsync();
        });

    /// <summary>Cria a fonte e o item, e programa o conteúdo que o download vai devolver.</summary>
    private async Task<CaptureItemId> SeedAsync(string sender, byte[]? content)
    {
        var sourceId = await ExecuteDbContextAsync(async db =>
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

        var artifactKey = $"anexo-{Guid.CreateVersion7():N}.pdf";

        if (content is not null)
        {
            var reader = _services.GetRequiredService<FakeMailboxReader>();
            reader.Artifacts[artifactKey] = content;
        }

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2THVSAAA=", artifactKey, sender, "Assunto", OccurredAt, OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId id)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id));
}
