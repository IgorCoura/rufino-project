namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// Degrau 3 da escada de roteamento: o número da conta cadastrado na expectativa (ADR-026).
/// </summary>
/// <remarks>
/// Conta de concessionária não imprime o documento do pagador, mas imprime a conta do cliente —
/// e na arrecadação ela está dentro do código de barras. Sem este degrau, a conta caía na
/// reivindicação todo mês, mesmo com o tenant tendo dito qual era o número dela.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class AccountReferenceRoutingTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly PayeeId Telefonica = PayeeId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e7"));
    private static readonly DateTime OccurredAt = new(2026, 9, 13, 3, 20, 0, DateTimeKind.Utc);

    /// <summary>Arrecadação cujo campo livre é <c>5678901234567890123456789</c>.</summary>
    private const string UtilityLine = "826600000010224812345672890123456786901234567898";

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private readonly IServiceProvider _services;

    public AccountReferenceRoutingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
    }

    // TESTE ÂNCORA. A conta cadastrada está dentro do código de barras da fatura: o boleto vira do
    // tenant com confiança forte, sem documento fiscal nenhum impresso.
    [Fact]
    public async Task Process_WhenTheRegisteredAccountIsInsideTheBarcode_ShouldPromoteAsStrong()
    {
        await SeedProfileAsync();
        await SeedExpectationAsync("901234567");
        var itemId = await SeedPdfAsync(PdfWith("Fatura de telefonia", UtilityLine));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Promote", result.Routing);
        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, item!.Status);
        Assert.Same(RoutingConfidence.Strong, item.Routing);
    }

    // A conta só no texto — "Nº da Conta: 00001123004411", como a Vivo imprime — promove com
    // confiança fraca: o boleto chega à aprovação, mas sinalizado. O rótulo seguinte na mesma
    // linha é o que encerra o número: o PdfPig emenda as linhas, e sem ele a conta colaria no
    // código de barras e deixaria de ser a sequência inteira.
    [Fact]
    public async Task Process_WhenTheRegisteredAccountAppearsOnlyInTheText_ShouldPromoteAsWeak()
    {
        await SeedProfileAsync();
        await SeedExpectationAsync("1123004411");
        var itemId = await SeedPdfAsync(PdfWith("Nº da Conta: 00001123004411 Vencimento", BankSlipLine));

        await ProcessAsync(itemId);

        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, item!.Status);
        Assert.Same(RoutingConfidence.Weak, item.Routing);
    }

    // A CONTRAPROVA: o mesmo documento, sem a conta cadastrada, continua indo para a reivindicação.
    [Fact]
    public async Task Process_WhenNoExpectationHasTheAccount_ShouldStayUnrouted()
    {
        await SeedProfileAsync();
        await SeedExpectationAsync("777777777");
        var itemId = await SeedPdfAsync(PdfWith("Fatura de telefonia", UtilityLine));

        await ProcessAsync(itemId);

        Assert.Same(CaptureItemStatus.Unrouted, (await LoadItemAsync(itemId))!.Status);
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

    private Task<CaptureItem?> LoadItemAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task SeedProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", "11222333000181", OccurredAt));
            await db.SaveEntitiesAsync();
        });

    private Task SeedExpectationAsync(string accountReference)
        => ExecuteDbContextAsync(async db =>
        {
            await db.BillExpectations.AddAsync(BillExpectation.Register(
                Tenant, Telefonica, accountReference, "Vivo", Recurrence.Monthly,
                expectedDueDay: 17, observedLeadDays: 4, alertLeadDays: null,
                anchorDueDate: null, hintSourceId: null, OccurredAt));
            await db.SaveEntitiesAsync();
        });

    private async Task<CaptureItemId> SeedPdfAsync(byte[] pdf)
    {
        var sourceId = await ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c3")),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

        const string artifactKey = "AAMkAGI2FATURAVIVO";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = pdf;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2CONTAAAA=", artifactKey,
                "contadigitalvivo@vivo.com.br", "Sua Fatura Digital Vivo Chegou", OccurredAt, OccurredAt,
                "application/pdf", fileName: "fatura.pdf");

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
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
}
