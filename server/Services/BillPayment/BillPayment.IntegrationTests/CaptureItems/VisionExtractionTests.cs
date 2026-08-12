namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Payees;
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
/// O degrau 3 da cascata: o extrator de visão propõe, o funil determinístico dispõe.
/// </summary>
/// <remarks>
/// <strong>O teste que mais importa aqui não é o que prova que a visão resolve um boleto</strong>
/// — é o que prova que uma linha digitável alucinada é barrada e o item NÃO vira boleto. Se
/// alguém um dia ligar a saída do modelo direto na <c>Bill</c>, é ele que quebra (ADR-011).
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class VisionExtractionTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";
    private const string KnownSender = "faturas@fornecedor.com.br";
    private const string UnknownSender = "marketing@desconhecido.com";

    private readonly IServiceProvider _services;
    private readonly FakeDocumentIntelligence _vision;

    public VisionExtractionTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
        _vision = _services.GetRequiredService<FakeDocumentIntelligence>();
    }

    // A visão resolve o que o determinístico não resolveu, e o degrau fica registrado — é o que
    // permite medir quanto da cascata está custando dinheiro.
    [Fact]
    public async Task Process_WhenVisionFindsAValidLine_ShouldParseAndRecordTheVisionStep()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Parsed, stored!.Status);
        Assert.Same(ExtractionMethod.Vision, stored.Extraction);
    }

    // TESTE ÂNCORA DO ADR-011. O modelo devolve uma linha com um dígito trocado — o erro típico
    // de leitura — e o dígito verificador a barra. O item continua na quarentena, e nenhum
    // boleto nasce de um número inventado.
    [Fact]
    public async Task Process_WhenVisionHallucinatesALine_ShouldNotProduceABill()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [Tamper(ValidBankSlip)]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrecognized, stored!.Status);
        Assert.Null(stored.Extraction);
    }

    // Sem remetente conhecido e sem sinal de cobrança, o extrator NÃO é chamado — é o portão que
    // mantém o gasto proporcional: dos 404 anexos medidos, 250 não tinham sinal algum.
    [Fact]
    public async Task Process_WithoutAnyBillingSignal_ShouldNotSpendTheExtractor()
    {
        var itemId = await SeedAsync(UnknownSender, "APRESENTAÇÃO DE CABOS ELETRICOS", PdfWith("catalogo"));

        await ProcessAsync(itemId);

        Assert.Equal(0, _vision.CallCount);
    }

    // Sinal de cobrança no assunto vale a chamada mesmo de remetente desconhecido — inclusive
    // quando não contém "boleto" nem "conta", que é o caso real de "Sua fatura chegou".
    [Fact]
    public async Task Process_WithABillingSignal_ShouldSpendEvenForAnUnknownSender()
    {
        var itemId = await SeedAsync(UnknownSender, "Sua fatura chegou", PdfWith("documento"));

        await ProcessAsync(itemId);

        Assert.Equal(1, _vision.CallCount);
    }

    // Anexo em IMAGEM chega ao extrator como imagem. A cascata determinística só abre PDF, e a
    // medição de 2026-08-11 recusou 12 anexos com not_a_pdf — baixados e nunca lidos.
    [Fact]
    public async Task Process_WithAnImageAttachment_ShouldStillReachTheExtractor()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto", [1, 2, 3, 4], contentType: "image/png");
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.Contains("image/png", _vision.ReceivedMediaTypes, StringComparer.Ordinal);
    }

    // As dicas levam o que o PRÓPRIO tenant cadastrou — documentos do pagador e nomes de
    // beneficiários. É escolha consciente do doc 10, e nada de outro tenant entra aqui.
    [Fact]
    public async Task Process_ShouldSendOnlyOwnTenantDataAsHints()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();
        await SeedPayeeAsync();

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento"));
        await ProcessAsync(itemId);

        var hints = _vision.LastHints;
        Assert.NotNull(hints);
        Assert.Contains("11222333000181", hints.PayerTaxIds, StringComparer.Ordinal);
        Assert.Contains("FORNECEDOR CONHECIDO SA", hints.KnownPayeeNames, StringComparer.Ordinal);
        Assert.Equal(KnownSender, hints.SenderAddress);
    }

    // Extrator desligado não muda nada: a cascata termina no determinístico, como antes da 2.4.
    [Fact]
    public async Task Process_WhenTheExtractorIsDisabled_ShouldFallBackToTheDeterministicOutcome()
    {
        await SeedTrustedOriginAsync();
        _vision.IsEnabled = false;

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento sem linha"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);
        Assert.Equal(0, _vision.CallCount);
    }

    // TESTE DE REGRESSÃO. O tipo vem do que o provedor DECLAROU, nunca da chave do artefato —
    // que no Graph é opaca. Sem tipo declarado o extrator não é chamado, em vez de receber o
    // arquivo rotulado como PDF por chute e o provedor recusar.
    [Fact]
    public async Task Process_WithoutADeclaredContentType_ShouldNotCallTheExtractor()
    {
        await SeedTrustedOriginAsync();

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento"), contentType: null);
        await ProcessAsync(itemId);

        Assert.Equal(0, _vision.CallCount);
    }

    // O portão de gasto examina o NOME do arquivo, não a chave opaca — senão o sinal de cobrança
    // no anexo nunca casaria com nada.
    [Fact]
    public async Task Process_WithABillingSignalInTheFileName_ShouldSpend()
    {
        var itemId = await SeedAsync(
            UnknownSender, "ENC:", PdfWith("documento"), fileName: "boleto-agosto.pdf");

        await ProcessAsync(itemId);

        Assert.Equal(1, _vision.CallCount);
    }

    /// <summary>Troca um dígito do meio — a alucinação típica, que "parece" a linha certa.</summary>
    private static string Tamper(string line)
    {
        var digits = line.ToCharArray();
        var middle = digits.Length / 2;
        digits[middle] = digits[middle] == '9' ? '8' : (char)(digits[middle] + 1);

        return new string(digits);
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

        return await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value));
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task SeedTrustedOriginAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var origin = TrustedOrigin.Register(
                Tenant, OriginKind.EmailAddress, KnownSender, TrustDecision.Trusted, Decider, note: null, OccurredAt);

            await db.TrustedOrigins.AddAsync(origin);
            await db.SaveEntitiesAsync();
        });

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var profile = PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", "11222333000181", OccurredAt);

            await db.PayerProfiles.AddAsync(profile);
            await db.SaveEntitiesAsync();
        });

    private Task SeedPayeeAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var payee = Payee.Register(
                Tenant, "FORNECEDOR CONHECIDO SA", "11444777000161", AmountPolicyKind.Unbounded,
                expectedAmount: null, tolerancePercent: null, minAmount: null, maxAmount: null, OccurredAt);

            await db.Payees.AddAsync(payee);
            await db.SaveEntitiesAsync();
        });

    private async Task<CaptureItemId> SeedAsync(
        string sender,
        string subject,
        byte[] content,
        string? contentType = "application/pdf",
        string? fileName = null)
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

        // Chave OPACA, como a do Graph: sem extensão, para nenhum teste passar por acidente
        // deduzindo o tipo dela.
        var artifactKey = $"AAMkAGI2{Guid.CreateVersion7():N}";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2THVSAAA=", artifactKey, sender, subject, OccurredAt, OccurredAt,
                contentType, fileName);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }
}
