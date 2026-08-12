namespace BillPayment.IntegrationTests.CaptureItems;

using System.Text;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
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
        Assert.Same(CaptureItemStatus.Parsed, stored!.Status);
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
        Assert.Same(CaptureItemStatus.Parsed, stored!.Status);
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
