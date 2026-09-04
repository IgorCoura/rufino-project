namespace BillPayment.IntegrationTests.Bills;

using BillPayment.Application.Bills.Commands;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O retrato da leitura por IA na <c>Bill</c> — a Fase C de 2026-08-27: todo candidato a boleto
/// passa pela visão, o retrato nasce junto com o boleto, e o backfill relê o acervo.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillReadingEnrichmentTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime OccurredAt = new(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";
    private const string TenantCnpj = "11222333000181";
    private const string KnownSender = "faturas@fornecedor.com.br";

    private readonly IServiceProvider _services;
    private readonly FakeDocumentIntelligence _vision;

    public BillReadingEnrichmentTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
        _vision = _services.GetRequiredService<FakeDocumentIntelligence>();
    }

    // O boleto resolvido pelo determinístico nasce com o retrato da IA: competência normalizada,
    // descrição e beneficiário lido — e o detalhe da API os expõe no bloco Reading.
    [Fact]
    public async Task Process_OnAResolvedDocument_ShouldAttachTheVisionReadingToTheBill()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();
        _vision.Result = ExtractedDocument.From(
            payeeName: "SABESP",
            billingPeriod: "07/2026",
            description: "Conta de água — instalação 12345");

        var itemId = await SeedAsync(PdfWith("Banco Itau", TenantCnpj, ValidBankSlip));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.NotNull(result.BillId);

        var bill = await LoadBillAsync(result.BillId!.Value);
        Assert.NotNull(bill.Reading);
        Assert.Equal("Conta de água — instalação 12345", bill.Reading!.Description);
        Assert.Equal(2026, bill.Reading.Competence!.Year);
        Assert.Equal(7, bill.Reading.Competence.Month);

        using var scope = _services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillQueries>();
        var detail = await queries.GetDetailAsync(Tenant.Value, result.BillId.Value);

        Assert.NotNull(detail!.Reading);
        Assert.Equal("07/2026", detail.Reading!.BillingPeriod);
        Assert.Equal(7, detail.Reading.CompetenceMonth);
    }

    // A captura nunca é refém da IA: modelo devolvendo vazio, o boleto nasce mesmo assim — sem
    // retrato, e com o vencimento da linha digitável intacto.
    [Fact]
    public async Task Process_WhenVisionReturnsNothing_ShouldStillPromoteWithoutAReading()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();

        var itemId = await SeedAsync(PdfWith("Banco Itau", TenantCnpj, ValidBankSlip));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.NotNull(result.BillId);
        Assert.Null((await LoadBillAsync(result.BillId!.Value)).Reading);
    }

    // O backfill, agora em DUAS etapas: pedir para reler ENFILEIRA, e quem lê é a fila. Chamar o
    // extrator dentro da requisição prendia o HTTP pela latência do provedor e devolvia "nada
    // extraído" quando na verdade o provedor tinha falhado — a confusão que esta sprint desfez.
    [Fact]
    public async Task Enrich_OnABillWithoutAReading_ShouldQueueAndThenBackfillFromTheStoredDocument()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();

        _vision.IsEnabled = false;
        var itemId = await SeedAsync(PdfWith("Banco Itau", TenantCnpj, ValidBankSlip));
        var processed = await ProcessAsync(itemId);
        Assert.NotNull(processed.BillId);
        Assert.Null((await LoadBillAsync(processed.BillId!.Value)).Reading);

        _vision.IsEnabled = true;
        _vision.Result = ExtractedDocument.From(billingPeriod: "agosto/2026", description: "Energia da sede");

        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var queued = await mediator.Send(new EnrichBillReadingCommand(Tenant.Value, processed.BillId.Value));

        Assert.True(queued.Enriched);
        Assert.Equal("queued", queued.Reason);
        Assert.Same(ReadingStatus.Queued, (await LoadBillAsync(processed.BillId.Value)).ReadingState);

        // A fila faz o trabalho — e é ela que anexa o retrato.
        var applied = await mediator.Send(new ApplyBillReadingCommand(Tenant.Value, processed.BillId.Value));

        Assert.True(applied.Applied);

        var bill = await LoadBillAsync(processed.BillId.Value);
        Assert.Same(ReadingStatus.Done, bill.ReadingState);
        Assert.NotNull(bill.Reading);
        Assert.Equal("Energia da sede", bill.Reading!.Description);
        Assert.Equal(8, bill.Reading.Competence!.Month);
    }

    // TESTE-ANCORA da fila: provedor fora do ar NAO marca o boleto como "sem o que ler" — ele
    // volta para a fila. Antes, uma falha de rede virava "nothing_extracted" e o boleto ficava
    // sem retrato para sempre, sem ninguem saber que a causa tinha sido um 503.
    [Fact]
    public async Task ApplyReading_WhenTheProviderIsUnavailable_ShouldAskToRequeueInsteadOfGivingUp()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();

        var itemId = await SeedAsync(PdfWith("Banco Itau", TenantCnpj, ValidBankSlip));
        var processed = await ProcessAsync(itemId);

        _vision.Outcome = ExtractionStatus.Unavailable;

        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new EnrichBillReadingCommand(Tenant.Value, processed.BillId!.Value));

        var failure = await Assert.ThrowsAsync<DomainException>(
            () => mediator.Send(new ApplyBillReadingCommand(Tenant.Value, processed.BillId.Value)));

        Assert.Equal("BLP.BIL28", failure.Id);

        // Continua na fila: quem decide desistir e o worker, depois de esgotar as tentativas.
        Assert.Same(ReadingStatus.Queued, (await LoadBillAsync(processed.BillId.Value)).ReadingState);
    }

    // TESTE DE REGRESSÃO (2026-08-28): quando o modelo responde e não traz nada, o boleto tem
    // que SAIR da fila — e a saída precisa ser PERSISTIDA. O desfecho `NothingExtracted` mudava
    // `ReadingState` para `Unavailable` no agregado e voltava sem `SaveEntitiesAsync`; não há
    // TransactionBehavior no pipeline, então a mudança morria no fim do escopo. O boleto seguia
    // `Queued`, o aluguel vencia em quinze minutos, a reivindicação o pegava de novo, o extrator
    // devolvia o mesmo nada — em laço, gastando cota do provedor a cada volta.
    [Fact]
    public async Task ApplyReading_WhenNothingIsExtracted_ShouldPersistTheBillLeavingTheQueue()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();

        var itemId = await SeedAsync(PdfWith("Banco Itau", TenantCnpj, ValidBankSlip));
        var processed = await ProcessAsync(itemId);

        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new EnrichBillReadingCommand(Tenant.Value, processed.BillId!.Value));
        Assert.Same(ReadingStatus.Queued, (await LoadBillAsync(processed.BillId.Value)).ReadingState);

        _vision.Result = ExtractedDocument.Empty;
        var applied = await mediator.Send(new ApplyBillReadingCommand(Tenant.Value, processed.BillId.Value));

        Assert.False(applied.Applied);
        Assert.Equal("NothingExtracted", applied.Outcome);

        // Lido de um contexto NOVO: é a releitura que prova que a saída da fila foi gravada, e
        // não apenas aplicada ao agregado em memória.
        Assert.Same(ReadingStatus.Unavailable, (await LoadBillAsync(processed.BillId.Value)).ReadingState);
    }

    // Documento escaneado resolvido pela visão roteia pelo degrau 1: o CNPJ do pagador que o
    // modelo leu (e o DV provou) sobe a escada — antes, o retrato era descartado e o item caía
    // na fila de reivindicação mesmo com o documento do tenant impresso no papel.
    [Fact]
    public async Task Process_WhenVisionReadsTheTenantTaxId_ShouldRouteWithStrongConfidence()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();
        _vision.Result = ExtractedDocument.From(
            digitableLineCandidates: [ValidBankSlip],
            payerTaxId: TenantCnpj);

        var itemId = await SeedAsync(PdfWith("documento escaneado sem camada de texto util"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.NotNull(result.BillId);

        var bill = await LoadBillAsync(result.BillId!.Value);
        Assert.Same(RoutingConfidence.Strong, bill.Routing);
        Assert.Equal(TenantCnpj, bill.ExtractedPayer!.TaxId!.Value);
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

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value));

        return first.Decision != "VisionPending"
            ? first
            : await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: true));
    }

    private Task<Bill> LoadBillAsync(Guid billId)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .SingleAsync(b => b.Id == BillId.From(billId)));

    private Task SeedTrustedOriginAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var origin = TrustedOrigin.Register(
                Tenant,
                OriginKind.EmailAddress,
                KnownSender,
                TrustDecision.Trusted,
                UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1")),
                note: null,
                OccurredAt);

            await db.TrustedOrigins.AddAsync(origin);
            await db.SaveEntitiesAsync();
        });

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var profile = PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", TenantCnpj, OccurredAt);

            await db.PayerProfiles.AddAsync(profile);
            await db.SaveEntitiesAsync();
        });

    private async Task<CaptureItemId> SeedAsync(byte[] content)
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

        var artifactKey = $"AAMkAGI2{Guid.CreateVersion7():N}";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2THVSAAA=", artifactKey, KnownSender, "Sua fatura chegou",
                OccurredAt, OccurredAt, "application/pdf", "boleto.pdf");

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }
}
