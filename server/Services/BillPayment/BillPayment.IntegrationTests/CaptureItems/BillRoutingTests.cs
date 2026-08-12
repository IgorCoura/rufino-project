namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// A escada de roteamento de ponta a ponta: de quem é o boleto que acabou de ser extraído.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o que fecha o funil.</strong> Até a 2.5 o sistema reconhecia boleto e parava —
/// nenhum <c>CaptureItem</c> virava <c>Bill</c>, e portanto nada chegava à verificação nem à
/// aprovação. Aqui o artefato atravessa até virar boleto do tenant certo, ou até parar na
/// quarentena por não se saber de quem é.
/// </para>
/// <para>
/// <strong>Nenhum boleto vira <c>Bill</c> sem rota determinada</strong>, e não existe atribuição
/// por default ao dono da fonte: numa caixa compartilhada chega a conta dos dois, e assumir que
/// é de quem conectou é exatamente como um usuário pagaria a conta do outro (doc 07).
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillRoutingTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId OtherTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly UserId Claimer = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";

    /// <summary>O CNPJ do tenant, como o emissor o imprime no bloco do pagador.</summary>
    private const string TenantCnpj = "11.222.333/0001-81";

    /// <summary>O CNPJ do beneficiário — presente em TODO boleto, e nunca do pagador.</summary>
    private const string PayeeCnpj = "11.444.777/0001-61";

    private const string SomeoneElsesCnpj = "45.997.418/0001-53";

    private const string KnownSender = "cobranca@fornecedor.com.br";

    private readonly WebApplicationFactory<Program> _host;
    private readonly IServiceProvider _services;

    private CaptureSourceId? _sourceId;

    public BillRoutingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        // O host irmão, e não a fábrica base: é ele que tem o armazenamento em memória. Com o
        // cliente da fábrica base a reivindicação cai no armazenamento não configurado, que
        // falha em toda leitura — de propósito, para o perfil de desenvolvimento não vazar
        // para a suíte.
        _host = factory.WithCaptureChain();
        _services = _host.Services;
    }

    // Degrau 1, o que resolve 93,3% dos boletos reais: o CNPJ do tenant impresso no documento
    // atribui o boleto a ele, e o item vira Bill com confiança Strong.
    [Fact]
    public async Task Process_WhenTheDocumentCarriesTheTenantTaxId_ShouldPromoteToBill()
    {
        await SeedPayerProfileAsync();
        var itemId = await SeedAsync(BoletoWith("Pagador", TenantCnpj));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Promote", result.Routing);
        Assert.NotNull(result.BillId);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, stored!.Status);
        Assert.Same(RoutingConfidence.Strong, stored.Routing);

        var bill = await LoadBillAsync(stored.BillId!.Value);
        Assert.Equal(Tenant, bill!.TenantId);
        Assert.Same(BillSourceKind.Mailbox, bill.Origin.SourceKind);

        // O pagador extraído alimenta o check PayerMatch, e tem que ser o documento QUE CASOU —
        // preenchê-lo com o CNPJ do credor reprovaria o boleto por contradizer o cadastro.
        Assert.Equal("11222333000181", bill.ExtractedPayer!.TaxId!.Value);
    }

    // TESTE ÂNCORA DA ASSIMETRIA ENTRE ATRIBUIR E RECUSAR. O documento traz o CNPJ do
    // BENEFICIÁRIO — como todo boleto —, e ele não é do tenant. Concluir "é de outra pessoa" daí
    // mandaria a conta para ForeignPayer, que NÃO expõe valor e NÃO pode ser reivindicada: o
    // usuário perderia a conta dele sem ter como recuperá-la. Sem rótulo, o desfecho é Unrouted.
    [Fact]
    public async Task Process_WhenTheOnlyTaxIdIsThePayees_ShouldQueueForClaimAndNotMarkAsForeign()
    {
        await SeedPayerProfileAsync();
        var itemId = await SeedAsync(BoletoWith("Beneficiario", PayeeCnpj));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Unrouted", result.Routing);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrouted, stored!.Status);
        Assert.Null(stored.BillId);
        Assert.Null(stored.Routing);
    }

    // Degrau 1 negativo: documento de terceiro SOB RÓTULO de pagador é a única evidência que
    // autoriza a quarentena cega — aí o sistema sabe que não é deste tenant.
    [Fact]
    public async Task Process_WhenALabelledPayerIsSomeoneElse_ShouldQuarantineAsForeign()
    {
        await SeedPayerProfileAsync();
        var itemId = await SeedAsync(BoletoWith("Pagador", SomeoneElsesCnpj));

        var result = await ProcessAsync(itemId);

        Assert.Equal("Foreign", result.Routing);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.ForeignPayer, stored!.Status);
        Assert.Null(stored.BillId);
    }

    // Degrau 3: o beneficiário é cadastrado só por este tenant. Promove, mas com confiança Weak
    // — o check TenantRouting sai Inconclusive e a aprovação humana continua obrigatória.
    [Fact]
    public async Task Process_WhenThePayeeIsExclusiveToThisTenant_ShouldPromoteAsWeak()
    {
        await SeedPayerProfileAsync();
        await SeedPayeeAsync(Tenant);

        var itemId = await SeedAsync(BoletoWith("Beneficiario", PayeeCnpj));

        await ProcessAsync(itemId);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, stored!.Status);
        Assert.Same(RoutingConfidence.Weak, stored.Routing);
    }

    // E o mesmo beneficiário cadastrado por DOIS tenants deixa de resolver: a evidência vira
    // ambígua, e escolher seria adivinhar de quem é a conta. Este é o teste que prova que a
    // travessia de tenant do ADR-008 está sendo consultada de verdade.
    [Fact]
    public async Task Process_WhenThePayeeIsAlsoRegisteredByAnotherTenant_ShouldQueueForClaim()
    {
        await SeedPayerProfileAsync();
        await SeedPayeeAsync(Tenant);
        await SeedPayeeAsync(OtherTenant);

        var itemId = await SeedAsync(BoletoWith("Beneficiario", PayeeCnpj));

        await ProcessAsync(itemId);

        Assert.Same(CaptureItemStatus.Unrouted, (await LoadAsync(itemId))!.Status);
    }

    // O MESMO boleto chegando em duas mensagens — medido na caixa real em 2026-08-11: quatro
    // anexos idênticos repetidos. Os dois itens apontam para o mesmo boleto em vez de o segundo
    // cair na quarentena por colidir com a unicidade global que o primeiro ocupa.
    [Fact]
    public async Task Process_WhenTheSameBoletoArrivesTwice_ShouldPointBothItemsToTheSameBill()
    {
        await SeedPayerProfileAsync();

        // Documentos diferentes, mesmo instrumento: é o reenvio da mesma cobrança, não o mesmo
        // arquivo (que a dedup por hash de conteúdo já resolve na ingestão).
        var first = await SeedAsync(BoletoWith("Pagador", TenantCnpj));
        var second = await SeedAsync(PdfWith(
            "Segunda via", "Pagador", $"CPF/CNPJ{TenantCnpj}", "LinhaDigitavel", ValidBankSlip));

        var firstResult = await ProcessAsync(first);
        var secondResult = await ProcessAsync(second);

        Assert.Equal(firstResult.BillId, secondResult.BillId);
        Assert.Same(CaptureItemStatus.Promoted, (await LoadAsync(second))!.Status);

        var count = await ExecuteDbContextAsync(db => db.Bills.CountAsync(b => b.TenantId == Tenant));
        Assert.Equal(1, count);
    }

    // A reivindicação promove o item e registra QUEM decidiu — a Bill nasce com confiança
    // Claimed, que é o que faz o check TenantRouting sair Inconclusive na tela de aprovação.
    [Fact]
    public async Task Claim_WhenTheItemIsUnrouted_ShouldCreateTheBillWithClaimedConfidence()
    {
        await SeedPayerProfileAsync();
        var itemId = await SeedAsync(BoletoWith("Beneficiario", PayeeCnpj));
        await ProcessAsync(itemId);

        var response = await ClaimOverHttpAsync(itemId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, stored!.Status);
        Assert.Same(RoutingConfidence.Claimed, stored.Routing);
        Assert.Equal(Claimer, stored.ClaimedBy);
        Assert.NotNull(stored.ClaimedAt);

        var bill = await LoadBillAsync(stored.BillId!.Value);
        Assert.Same(RoutingConfidence.Claimed, bill!.Routing);

        // A reivindicação escolhe de QUEM é o boleto, nunca o que ele diz: o pagador continua
        // não extraído, e o check PayerMatch fica Inconclusive em vez de falsamente aprovado.
        Assert.Null(bill.ExtractedPayer);
    }

    // A escada já sabia que o pagador é outro, e a reivindicação não pode sobrepor a única
    // evidência CONSTATADA de propriedade — BLP.CPI04, o erro fixado pelo doc 07.
    [Fact]
    public async Task Claim_WhenTheExtractedPayerContradictsTheTenant_ShouldBeRefused()
    {
        await SeedPayerProfileAsync();
        var itemId = await SeedAsync(BoletoWith("Pagador", SomeoneElsesCnpj));
        await ProcessAsync(itemId);

        var response = await ClaimOverHttpAsync(itemId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.CPI04", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        Assert.Same(CaptureItemStatus.ForeignPayer, (await LoadAsync(itemId))!.Status);
    }

    // Exceção 2 do doc 07: o mesmo boleto já está sob gestão de outra conta. O aviso é genérico
    // — o usuário precisa saber que existe, nunca de quem é.
    [Fact]
    public async Task Claim_WhenTheBillIsAlreadyUnderAnotherAccount_ShouldRefuseWithoutNamingIt()
    {
        await SeedPayerProfileAsync();
        await SeedForeignBillAsync();

        var itemId = await SeedAsync(BoletoWith("Beneficiario", PayeeCnpj));
        await ProcessAsync(itemId);

        var response = await ClaimOverHttpAsync(itemId);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.BIL02", body, StringComparison.Ordinal);
        Assert.DoesNotContain(OtherTenant.Value.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Um boleto sintético no formato que o PdfPig entrega o texto de um documento real.
    /// </summary>
    /// <remarks>
    /// <strong>Os rótulos entre os campos não são enfeite do fixture.</strong> O
    /// <c>page.Text</c> do PdfPig concatena os blocos SEM separador, e num boleto de verdade
    /// sempre há uma palavra entre um número e o seguinte — medido em documentos reais do
    /// corpus: <c>CPF/CNPJ21.692.055/0001-80Registro2506564</c>. É a letra que encerra a
    /// sequência de dígitos, e é dela que a leitura de documento fiscal depende para não
    /// emendar o CNPJ do pagador na linha digitável.
    /// </remarks>
    private static byte[] BoletoWith(string label, string taxId)
        => PdfWith(label, $"CPF/CNPJ{taxId}", "LinhaDigitavel", ValidBankSlip, "Vencimento");

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

    private Task<HttpResponseMessage> ClaimOverHttpAsync(CaptureItemId itemId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/{Tenant.Value}/capture-items/{itemId.Value}/claim");

        request.Headers.Add("x-user-id", Claimer.Value.ToString());
        request.Headers.Add("x-requestid", Guid.CreateVersion7().ToString());
        request.Content = JsonContent.Create(new { });

        return _host.CreateClient().SendAsync(request);
    }

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value));
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task<Bill?> LoadBillAsync(BillId billId)
        => ExecuteDbContextAsync(db => db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == billId));

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                Tenant, PayerKind.Company, "RUFINO EMPREITEIRA LTDA", TaxId.Parse(TenantCnpj), OccurredAt));

            await db.SaveEntitiesAsync();
        });

    private Task SeedPayeeAsync(TenantId tenantId)
        => ExecuteDbContextAsync(async db =>
        {
            await db.Payees.AddAsync(Payee.Register(
                tenantId,
                "CONCESSIONARIA EXEMPLO SA",
                TaxId.Parse(PayeeCnpj),
                AmountPolicy.Unbounded(),
                OccurredAt));

            await db.SaveEntitiesAsync();
        });

    /// <summary>O mesmo instrumento já capturado por outra conta — ocupa a chave global.</summary>
    private Task SeedForeignBillAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var bill = Bill.Capture(
                OtherTenant,
                [Domain.Instruments.PaymentInstrument.FromBarcode(
                    Domain.Instruments.DigitableLine.Parse(ValidBankSlip, OccurredAt))],
                BillOrigin.Create(BillSourceKind.ManualUpload, OccurredAt, storageKey: "outra-conta"),
                OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
        });

    /// <summary>
    /// A fonte é uma só por teste: <c>(tenant, endereço)</c> é único, então semear uma fonte
    /// nova a cada item colidiria no índice antes de o teste chegar ao que ele mede.
    /// </summary>
    private async Task<CaptureSourceId> EnsureSourceAsync()
    {
        if (_sourceId is { } existing)
            return existing;

        _sourceId = await ExecuteDbContextAsync(async db =>
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

        return _sourceId.Value;
    }

    private async Task<CaptureItemId> SeedAsync(byte[] content)
    {
        var sourceId = await EnsureSourceAsync();

        var artifactKey = $"AAMkAGI2{Guid.CreateVersion7():N}";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, $"AAMkAGI2{Guid.CreateVersion7():N}", artifactKey, KnownSender,
                "Boleto de agosto", OccurredAt, OccurredAt, "application/pdf", "boleto.pdf");

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }
}
