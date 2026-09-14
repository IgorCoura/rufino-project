namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Payees;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// "Lembrar desta conta" (ADR-026): a reivindicação ensina o número da conta, e o mês seguinte chega
/// roteado em vez de voltar para a fila.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RememberAccountOnClaimTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Claimer = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));
    private static readonly DateTime OccurredAt = new(2026, 9, 13, 3, 20, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string Account = "1123004411";

    private readonly WebApplicationFactory<Program> _host;
    private readonly IServiceProvider _services;
    private readonly FakeDocumentIntelligence _vision;

    private CaptureSourceId _sourceId;

    public RememberAccountOnClaimTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithCaptureChain();
        _services = _host.Services;
        _vision = _services.GetRequiredService<FakeDocumentIntelligence>();
        _vision.Result = ExtractedDocument.Empty;
    }

    // A IA leu o número da conta, e os dígitos estão no documento: a reivindicação já chega com a
    // sugestão pronta para ser lembrada.
    [Fact]
    public async Task Process_WhenTheAccountReadByVisionIsInTheDocument_ShouldSuggestIt()
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: "00001123004411");

        var item = await LoadItemAsync(itemId);

        Assert.Same(CaptureItemStatus.Unrouted, item!.Status);
        Assert.Equal(Account, item.AccountReferenceSuggestion);
    }

    // A CONTRAPROVA: número lido pela IA que não está no documento não vira sugestão — o modelo lê
    // "Número da fatura" como conta com frequência, e sugerir marcado por padrão seria um convite ao
    // erro.
    [Fact]
    public async Task Process_WhenTheAccountReadByVisionIsNotInTheDocument_ShouldNotSuggestIt()
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: "987654321");

        Assert.Null((await LoadItemAsync(itemId))!.AccountReferenceSuggestion);
    }

    // Reivindicar pedindo para lembrar a conta presente no documento registra o pedido no item.
    [Fact]
    public async Task Claim_WithAnAccountThatIsInTheDocument_ShouldRecordTheRequest()
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: null);

        var response = await ClaimOverHttpAsync(itemId, "0000.1123004411");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, item!.Status);
        Assert.Equal(Account, item.RememberedAccountReference);
    }

    // Número que não está no documento recusa a reivindicação inteira com 400 BLP.CPI18 — digitado
    // errado, ele rotearia a conta de outra pessoa.
    [Fact]
    public async Task Claim_WithAnAccountThatIsNotInTheDocument_ShouldReturnBadRequestWith_BLP_CPI18()
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: null);

        var response = await ClaimOverHttpAsync(itemId, "555555555");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("BLP.CPI18", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Same(CaptureItemStatus.Unrouted, (await LoadItemAsync(itemId))!.Status);
    }

    // Número com menos de seis dígitos não é lembrado: 400 BLP.CPI19.
    [Fact]
    public async Task Claim_WithAShortAccount_ShouldReturnBadRequestWith_BLP_CPI19()
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: null);

        var response = await ClaimOverHttpAsync(itemId, "L4411");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("BLP.CPI19", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // TESTE ÂNCORA. Com o beneficiário resolvido, o pedido vira uma expectativa com o número da conta
    // — e cumpri-lo de novo (reentrega do outbox) não cria uma segunda.
    [Fact]
    public async Task Remember_WhenThePayeeIsResolved_ShouldCreateTheExpectationOnce()
    {
        var billId = await ClaimAndResolvePayeeAsync(registerPayee: true);

        var first = await RememberAsync(billId);
        var second = await RememberAsync(billId);

        Assert.Equal(RememberClaimedAccountCommandHandler.OUTCOME_CREATED, first.Outcome);
        Assert.Equal(RememberClaimedAccountCommandHandler.OUTCOME_ALREADY_REMEMBERED, second.Outcome);

        var expectation = Assert.Single(await ExecuteDbContextAsync(db => db.BillExpectations.AsNoTracking().ToListAsync()));
        Assert.Equal(Account, expectation.AccountReference);
        Assert.Equal(_sourceId, expectation.HintSourceId);
    }

    // A expectativa sem número que o beneficiário já tinha ganha o número — em vez de nascer uma
    // gêmea ao lado dela, com alerta em dobro.
    [Fact]
    public async Task Remember_WhenThePayeeHasOneExpectationWithoutAccount_ShouldFillIt()
    {
        var billId = await ClaimAndResolvePayeeAsync(registerPayee: true, seedBlankExpectation: true);

        var result = await RememberAsync(billId);

        Assert.Equal(RememberClaimedAccountCommandHandler.OUTCOME_FILLED_EXISTING, result.Outcome);
        var expectation = Assert.Single(await ExecuteDbContextAsync(db => db.BillExpectations.AsNoTracking().ToListAsync()));
        Assert.Equal(Account, expectation.AccountReference);
    }

    // Sem beneficiário cadastrado o pedido fica pendente, e o detalhe do boleto diz o que falta.
    [Fact]
    public async Task Remember_WhenThePayeeIsNotRegistered_ShouldStayPendingAndShowOnTheDetail()
    {
        var billId = await ClaimAndResolvePayeeAsync(registerPayee: false);

        var result = await RememberAsync(billId);

        Assert.Equal(RememberClaimedAccountCommandHandler.OUTCOME_AWAITING_PAYEE, result.Outcome);
        Assert.Empty(await ExecuteDbContextAsync(db => db.BillExpectations.AsNoTracking().ToListAsync()));

        using var scope = _services.CreateScope();
        var detail = await scope.ServiceProvider.GetRequiredService<IBillQueries>().GetDetailAsync(Tenant.Value, billId);
        Assert.Equal(Account, detail!.PendingAccountReference);
    }

    private async Task<Guid> ClaimAndResolvePayeeAsync(bool registerPayee, bool seedBlankExpectation = false)
    {
        var itemId = await SeedUnroutedAsync(accountReadByVision: null);
        var response = await ClaimOverHttpAsync(itemId, Account);
        response.EnsureSuccessStatusCode();

        var billId = (await LoadItemAsync(itemId))!.BillId!.Value;

        if (!registerPayee)
            return billId.Value;

        await ExecuteDbContextAsync(async db =>
        {
            var payee = Payee.Register(
                Tenant, "TELEFONICA BRASIL S.A.", TaxId.Parse("11444777000161"), AmountPolicy.Unbounded(), OccurredAt);
            await db.Payees.AddAsync(payee);

            var bill = await db.Bills.FirstAsync(b => b.Id == billId);
            bill.ResolvePayee(payee.Id, OccurredAt);

            if (seedBlankExpectation)
            {
                await db.BillExpectations.AddAsync(BillExpectation.Register(
                    Tenant, payee.Id, accountReference: null, "Vivo", Recurrence.Monthly,
                    expectedDueDay: 17, observedLeadDays: 4, alertLeadDays: null,
                    anchorDueDate: null, hintSourceId: null, OccurredAt));
            }

            await db.SaveEntitiesAsync();
        });

        return billId.Value;
    }

    private async Task<RememberClaimedAccountResponse> RememberAsync(Guid billId)
    {
        using var scope = _services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new RememberClaimedAccountCommand(Tenant.Value, billId));
    }

    private Task<HttpResponseMessage> ClaimOverHttpAsync(CaptureItemId itemId, string? rememberAccountReference)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/{Tenant.Value}/capture-items/{itemId.Value}/claim");

        request.Headers.Add("x-user-id", Claimer.Value.ToString());
        request.Headers.Add("x-requestid", Guid.CreateVersion7().ToString());
        request.Content = JsonContent.Create(new { rememberAccountReference });

        return _host.CreateClient().Authenticated().SendAsync(request);
    }

    private Task<CaptureItem?> LoadItemAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    /// <summary>
    /// Um item na fila de reivindicação com a conta impressa: sem cadastro fiscal nenhum, a escada
    /// não tem como atribuí-lo. O rótulo depois do número é o que o encerra no texto emendado.
    /// </summary>
    private async Task<CaptureItemId> SeedUnroutedAsync(string? accountReadByVision)
    {
        _vision.Result = ExtractedDocument.From(accountReference: accountReadByVision, description: "Fatura");

        _sourceId = await ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c4")),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

        const string artifactKey = "AAMkAGI2LEMBRAR";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] =
            PdfWith("Nº da Conta: 00001123004411 Vencimento", BankSlipLine);

        var itemId = await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, _sourceId, "AAMkAGI2LEMBRARAAA=", artifactKey,
                "contadigitalvivo@vivo.com.br", "Sua Fatura Digital Vivo Chegou", OccurredAt, OccurredAt,
                "application/pdf", fileName: "fatura.pdf");

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: false));
        if (first.Decision == "VisionPending")
            await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: true));

        return itemId;
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
