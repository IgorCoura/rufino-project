namespace BillPayment.IntegrationTests.CaptureItems;

using System.Text;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
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
/// Os degraus 1 e 2 da cascata: o instrumento escrito no corpo, e o documento atrás de um link.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A sprint 2.5 estava desenhada como "escada de link" e a medição contrariou o plano.</strong>
/// Varrendo um ano da caixa real em 2026-08-11, dois dos cinco arquétipos de conta por link já
/// traziam o dado pagável escrito no texto do e-mail: a SABESP manda o BR Code inteiro no formato
/// novo e a linha digitável de arrecadação no formato antigo. Os dois resolvem sem abrir arquivo e
/// <strong>sem tocar a rede</strong> — mais barato, e sem superfície de ataque nenhuma.
/// </para>
/// <para>
/// <strong>O que veio da rede não dispensa degrau nenhum.</strong> Um PDF trazido por link
/// atravessa exatamente a mesma cascata determinística de um anexo: DV da linha, CRC do BR Code,
/// senha derivada. O teste da linha adulterada é o que prova isso (ADR-011).
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class EmailBodyExtractionTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";
    private const string KnownSender = "contadigital@concessionaria.com.br";

    /// <summary>BR Code dinâmico, no formato que a concessionária escreve no corpo do e-mail.</summary>
    private const string PixPayload =
        "00020101021226770014BR.GOV.BCB.PIX2555api.itau/pix/qr/v2/fe80130d-c5ef-407b-94a5-f6b2005020095"
        + "204000053039865802BR5906SABESP6009SAO PAULO62070503***6304A76E";

    /// <summary>Emissor real sem receita cadastrada, do caso medido em 2026-08-26.</summary>
    private const string UnknownIssuerUrl = "https://www.asaas.com/i/55p08vsad5vci3g7";

    /// <summary>O CNPJ do tenant, para a escada promover o boleto anexado à mão.</summary>
    private const string TenantCnpjForAttach = "11222333000181";

    private const string BillUrl = "https://ssl.exemplo.com.br/Bill/8a467507-e583-44e6-b2ee-62207d1c0438";

    private readonly IServiceProvider _services;
    private readonly FakeDocumentLinkResolver _links;

    public EmailBodyExtractionTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
        _links = _services.GetRequiredService<FakeDocumentLinkResolver>();
    }

    // O BR Code escrito no corpo resolve sozinho, e o degrau fica registrado como EmailBody —
    // é a métrica que diz que o passo mais barato bastou.
    [Fact]
    public async Task Process_WhenBodyCarriesAPixPayload_ShouldParseWithoutTouchingTheNetwork()
    {
        var itemId = await SeedBodyAsync(HtmlWith($"<td>PAGUE COM PIX</td><td>{PixPayload}</td>"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);

        var stored = await LoadAsync(itemId);
        // `Parsed` virou estado de PASSAGEM na 2.6: a escada de roteamento roda logo depois da
        // cascata, e sem `PayerProfile` cadastrado ela não tem contra o quê comparar — o item
        // segue para a fila de reivindicação. O que este teste mede é o degrau da cascata, e
        // esse não mudou.
        Assert.Same(CaptureItemStatus.Unrouted, stored!.Status);
        Assert.Same(ExtractionMethod.EmailBody, stored.Extraction);

        // O degrau de link nem foi consultado: buscar o PDF de uma fatura cujo Pix já está no
        // corpo seria gastar rede para descobrir o que estava escrito ali.
        Assert.Equal(0, _links.CallCount);
        Assert.Null(stored.SourceUrl);
    }

    // A linha digitável de arrecadação também vem escrita no corpo, com a formatação em blocos
    // que o emissor usa para deixá-la legível.
    [Fact]
    public async Task Process_WhenBodyCarriesADigitableLine_ShouldParseFromTheBody()
    {
        var formatted = string.Join(' ', ValidBankSlip.Chunk(11).Select(c => new string(c)));
        var itemId = await SeedBodyAsync(HtmlWith($"<td>Código de barras:<br />{formatted}</td>"));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.Same(ExtractionMethod.EmailBody, (await LoadAsync(itemId))!.Extraction);
    }

    // TESTE DE REGRESSÃO DO CONVERSOR DE HTML. E-mail de cobrança é montado por editor visual e
    // parte a linha em vários <span>. Se toda tag virasse quebra de linha, o número seria cortado
    // ao meio e nunca fecharia o DV — a tag INLINE tem que desaparecer sem separar os dígitos.
    [Fact]
    public async Task Process_WhenTheLineIsSplitAcrossInlineTags_ShouldStillParseIt()
    {
        var split = string.Concat(ValidBankSlip.Chunk(9).Select(c => $"<span>{new string(c)}</span>"));
        var itemId = await SeedBodyAsync(HtmlWith($"<td>{split}</td>"));

        Assert.Equal("Parse", (await ProcessAsync(itemId)).Decision);
    }

    // Corpo que só tem link vai para o degrau 2, e o documento buscado atravessa a cascata
    // determinística como qualquer anexo. A procedência fica registrada.
    [Fact]
    public async Task Process_WhenTheBoletoIsBehindALink_ShouldFetchItAndRecordTheSourceUrl()
    {
        _links.Result = ResolvedDocument.From(PdfWith(ValidBankSlip), "application/pdf", BillUrl);

        var itemId = await SeedBodyAsync(HtmlWith($"""<a href="{BillUrl}">Acessar Boleto</a>"""));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);

        var stored = await LoadAsync(itemId);
        // `Parsed` virou estado de PASSAGEM na 2.6: a escada de roteamento roda logo depois da
        // cascata, e sem `PayerProfile` cadastrado ela não tem contra o quê comparar — o item
        // segue para a fila de reivindicação. O que este teste mede é o degrau da cascata, e
        // esse não mudou.
        Assert.Same(CaptureItemStatus.Unrouted, stored!.Status);
        Assert.Equal(BillUrl, stored.SourceUrl);

        // O degrau relatado é o do documento BUSCADO, não o do corpo: quem achou a linha foi o
        // texto embutido no PDF.
        Assert.Same(ExtractionMethod.EmbeddedText, stored.Extraction);
    }

    // TESTE ÂNCORA. O documento veio da rede, mas o dígito verificador continua mandando: uma
    // linha com um dígito trocado NÃO vira boleto só por ter sido baixada de um host autorizado.
    [Fact]
    public async Task Process_WhenTheFetchedDocumentCarriesATamperedLine_ShouldNotProduceABill()
    {
        await SeedTrustedOriginAsync();
        _links.Result = ResolvedDocument.From(PdfWith(Tamper(ValidBankSlip)), "application/pdf", BillUrl);

        var itemId = await SeedBodyAsync(HtmlWith($"""<a href="{BillUrl}">Acessar Boleto</a>"""));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrecognized, stored!.Status);
        Assert.Null(stored.Extraction);

        // A procedência é registrada mesmo sem resolver: saber ONDE o sistema foi procurar é o
        // que permite corrigir a receita depois.
        Assert.Equal(BillUrl, stored.SourceUrl);
    }

    // O degrau 2 opera sobre o CORPO do e-mail, não sobre um anexo — é do corpo que saem os links.
    [Fact]
    public async Task Process_WhenTheBodyHasNoInstrument_ShouldHandTheBodyToTheLinkResolver()
    {
        var itemId = await SeedBodyAsync(HtmlWith($"""<a href="{BillUrl}">Acessar Boleto</a>"""));

        await ProcessAsync(itemId);

        Assert.Equal(1, _links.CallCount);
        Assert.Contains(BillUrl, _links.LastBody, StringComparison.Ordinal);
    }

    // Anexo continua indo para o parser de PDF: rotear por tipo não pode ter quebrado o caminho
    // que já existia.
    [Fact]
    public async Task Process_WithAPdfAttachment_ShouldStillUseTheDocumentParser()
    {
        var itemId = await SeedAsync(
            KnownSender, "Boleto de agosto", PdfWith(ValidBankSlip), "application/pdf");

        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.Same(ExtractionMethod.EmbeddedText, (await LoadAsync(itemId))!.Extraction);
        Assert.Equal(0, _links.CallCount);
    }

    private static string HtmlWith(string inner)
        => $"<html><body><table><tbody><tr>{inner}</tr></tbody></table></body></html>";

    /// <summary>Troca um dígito do meio — o erro típico de leitura, que "parece" a linha certa.</summary>
    private static string Tamper(string line)
    {
        var digits = line.ToCharArray();
        var middle = digits.Length / 2;
        digits[middle] = digits[middle] == '9' ? '8' : (char)(digits[middle] + 1);

        return new string(digits);
    }

    // TESTE ÂNCORA (2026-08-26): a cobrança de um emissor SEM receita não desaparece mais.
    // O caso real foi a Asaas — assunto "uma cobrança foi gerada para você", sem anexo, com o
    // boleto atrás de www.asaas.com/i/{token}. Antes: nenhum sinal (host desconhecido), nenhum
    // artefato, e a mensagem sumia como "sem documento". Agora vira item, a escada tenta, e o
    // que ela não busca fica na quarentena para uma pessoa reivindicar.
    [Fact]
    public async Task Process_WhenAnUnknownIssuerSendsABillByLink_ShouldQuarantineInsteadOfVanishing()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedAsync(
            "cobrancas+6292297@asaas.com",
            "Olá, uma cobrança foi gerada para você",
            Encoding.UTF8.GetBytes($"<a href=\"{UnknownIssuerUrl}\">Ver cobrança</a>"),
            "text/html");

        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Unrecognized, item!.Status);
    }

    // E o item guarda de ONDE veio: é isso que transforma a quarentena na fila de receitas a
    // cadastrar. Sem o host, o item cai lá sem dizer qual emissor o sistema não sabe buscar.
    [Fact]
    public async Task Process_WhenTheLinkHasNoRecipe_ShouldRecordTheHostItWouldHaveFetched()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedAsync(
            "cobrancas+6292297@asaas.com",
            "Olá, uma cobrança foi gerada para você",
            Encoding.UTF8.GetBytes($"<a href=\"{UnknownIssuerUrl}\">Ver cobrança</a>"),
            "text/html");

        await ProcessAsync(itemId);

        var item = await LoadAsync(itemId);
        Assert.Equal("www.asaas.com", item!.LinkHost);
    }

    // A contraprova que mantém a fila utilizável: propaganda com link e sem sinal de cobrança
    // continua sendo descartada. Sem ela, a mudança viraria "guarde tudo".
    [Fact]
    public async Task Process_WhenAnUnknownLinkCarriesNoBillingSignal_ShouldStillDrop()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate("https://www.loja.com.br/oferta")!);

        var itemId = await SeedAsync(
            "marketing@loja.com.br",
            "72x num seminovo que vale a pena",
            Encoding.UTF8.GetBytes("<a href=\"https://www.loja.com.br/oferta\">Ver</a>"),
            "text/html");

        var result = await ProcessAsync(itemId);

        Assert.Equal("Drop", result.Decision);
        Assert.Null(await LoadAsync(itemId));
    }

    // TESTE ÂNCORA (2026-08-27): o boleto que o sistema não conseguiu buscar entra pela mão da
    // pessoa e vira `Bill`. É o que fecha o caminho de emissor sem receita — ela abre a URL que
    // a quarentena agora mostra, baixa o PDF e o devolve; daí em diante o fluxo é o de sempre.
    [Fact]
    public async Task AttachArtifact_OnAQuarantinedItem_ShouldTurnItIntoABill()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        await SeedPayerProfileAsync();

        var itemId = await SeedUnreachableBillAsync();
        await ProcessAsync(itemId);

        await AttachAsync(itemId, PdfWith("Pagador", TenantCnpjForAttach, ValidBankSlip));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Promoted, item!.Status);
        Assert.NotNull(item.BillId);

        // Quem trouxe o arquivo foi uma pessoa; QUEM leu o instrumento foi a cascata. São dois
        // fatos, e o item registra os dois separadamente.
        Assert.True(item.ManuallySupplied);
        Assert.Equal(ExtractionMethod.EmbeddedText, item.Extraction);
    }

    // E a procedência sobrevive: a URL de onde a pessoa tirou o documento continua registrada.
    // O reprocessamento comum a apaga — aqui ela é prova, não hipótese.
    [Fact]
    public async Task AttachArtifact_ShouldPreserveTheSourceUrl()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedUnreachableBillAsync();
        await ProcessAsync(itemId);

        await AttachAsync(itemId, PdfWith("Pagador", TenantCnpjForAttach, ValidBankSlip));
        await ProcessAsync(itemId);

        var item = await LoadAsync(itemId);
        Assert.Equal(UnknownIssuerUrl, item!.SourceUrl);
    }

    // CONTRAPROVA: com anexo manual o leitor de caixa NÃO é chamado. Rebaixar do e-mail traria de
    // volta o corpo que não tinha o boleto, desfazendo o trabalho da pessoa.
    [Fact]
    public async Task AttachArtifact_ShouldNotGoBackToTheMailbox()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedUnreachableBillAsync();
        await ProcessAsync(itemId);

        var reader = _services.GetRequiredService<FakeMailboxReader>();
        await AttachAsync(itemId, PdfWith("Pagador", TenantCnpjForAttach, ValidBankSlip));

        var before = reader.DownloadCount;
        await ProcessAsync(itemId);

        Assert.Equal(before, reader.DownloadCount);
    }

    // Anexo que não é boleto volta à quarentena — MAS o arquivo fica. Alguém escolheu subi-lo, e
    // apagá-lo jogaria fora o trabalho dela; a retenção por desfecho não alcança anexo manual.
    [Fact]
    public async Task AttachArtifact_WhenTheDocumentIsNotABill_ShouldKeepTheFile()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedUnreachableBillAsync();
        await ProcessAsync(itemId);

        await AttachAsync(itemId, PdfWith("Contrato de locacao", "Clausula primeira"));
        await ProcessAsync(itemId);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Unrecognized, item!.Status);
        Assert.True(item.ManuallySupplied);
    }

    // Reprovar tira o item da fila e registra quem decidiu.
    [Fact]
    public async Task Dismiss_ShouldRemoveTheItemFromTheQueueWithAnAuthor()
    {
        _links.Result = null;
        _links.Harvested.Add(DocumentLink.TryCreate(UnknownIssuerUrl)!);

        var itemId = await SeedUnreachableBillAsync();
        await ProcessAsync(itemId);

        await DismissAsync(itemId, "nao reconheco esta cobranca");

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Dismissed, item!.Status);
        Assert.Equal(Decider, item.DismissedBy);
        Assert.Equal("nao reconheco esta cobranca", item.Reason);
    }

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var profile = PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", TenantCnpjForAttach, OccurredAt);

            await db.PayerProfiles.AddAsync(profile);
            await db.SaveEntitiesAsync();
        });

    private Task<CaptureItemId> SeedUnreachableBillAsync()
        => SeedAsync(
            "cobrancas+6292297@asaas.com",
            "Olá, uma cobrança foi gerada para você",
            Encoding.UTF8.GetBytes($"<a href=\"{UnknownIssuerUrl}\">Ver cobrança</a>"),
            "text/html");

    private async Task AttachAsync(CaptureItemId itemId, byte[] content)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new AttachCaptureItemArtifactCommand(
            Tenant.Value, itemId.Value, content, "application/pdf", "boleto.pdf"));
    }

    private async Task DismissAsync(CaptureItemId itemId, string? note)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new DismissCaptureItemCommand(
            Tenant.Value, itemId.Value, Decider.Value, note));
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

    private Task<CaptureItemId> SeedBodyAsync(string html)
        => SeedAsync(KnownSender, "Chegou sua nova fatura digital", Encoding.UTF8.GetBytes(html), "text/html");

    private async Task<CaptureItemId> SeedAsync(string sender, string subject, byte[] content, string? contentType)
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
                Tenant, sourceId, "AAMkAGI2THVSAAA=", artifactKey, sender, subject, OccurredAt, OccurredAt,
                contentType, fileName: null);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }
}
