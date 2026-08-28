namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Secrets;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ImportBillTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid OtherTenantId = new("0195a1f0-0000-7000-8000-000000000002");
    private static readonly DateTime ReceivedAt = new(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);

    // Fontes REAIS, uma por tenant: desde 2026-08-28 o import valida o SourceId contra as fontes
    // do próprio tenant, e um Guid inventado responde 404.
    private Guid SourceId;
    private Guid OtherSourceId;

    // Instrumentos sintéticos com DVs e CRC corretos — instrumento real não entra no repositório.
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string OtherBankSlipLine = "03399876534321098765743210987657414930000140980";
    private const string UtilityLine = "826600000010224812345672890123456786901234567898";

    private const string StaticPix =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/bills", UriKind.Relative);

    private static Uri ImportRouteFor(Guid tenantId) => new($"{RouteFor(tenantId)}/import", UriKind.Relative);

    public ImportBillTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    public override async Task InitializeAsync()
    {
        SourceId = await SeedSourceAsync(TenantId);
        OtherSourceId = await SeedSourceAsync(OtherTenantId);
    }

    // Regressão (auditoria 2026-08-28): a proveniência era forjável — qualquer Guid entrava como
    // "fonte de captura". Fonte que não existe é 404, como qualquer referência a outro agregado.
    [Fact]
    public async Task PostImport_WithAnUnknownSource_ShouldReturnNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, new Guid("0195a1f0-0000-7000-8000-0000000000d1"), "a@b.com"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.CPS02", error!.Id);
    }

    // A fonte de OUTRO tenant não existe para este — 404, sem distinguir de "nunca existiu".
    [Fact]
    public async Task PostImport_WithAnotherTenantsSource_ShouldReturnNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, OtherSourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Regressão (auditoria 2026-08-28): o corpo JSON aceitava storageKey e contentHash — quem tem
    // bill:import apontava a "evidência" do boleto para qualquer objeto do balde. Os campos não
    // existem mais no contrato; mandá-los é ignorado pelo binder e a origem nasce sem arquivo.
    [Fact]
    public async Task PostImport_WithStorageKeyAndContentHashInTheBody_ShouldIgnoreThem()
    {
        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new
            {
                digitableLine = BankSlipLine,
                sourceKind = "ManualUpload",
                receivedAt = ReceivedAt,
                storageKey = $"tenants/{TenantId:N}/captures/qualquer-objeto.pdf",
                contentHash = "sha256:forjado",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();

        var bill = await ExecuteDbContextAsync(db => db.Bills.AsNoTracking().SingleAsync(b => b.Id == BillId.From(body!.Id)));
        Assert.Null(bill.Origin.StorageKey);
        Assert.Null(bill.Origin.ContentHash);
    }

    // Importar por código de barras persiste o boleto no trilho Boleto, como cobrança.
    [Fact]
    public async Task PostImport_WithBarcode_ShouldPersistOnTheBoletoRail()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "financeiro@fornecedor.com.br"));

        Assert.Equal("BankSlip", body.Kind);
        Assert.Equal("Boleto", body.Rail);

        var persisted = await ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .SingleAsync(b => b.Id == BillId.From(body.Id)));

        Assert.Same(BillStatus.Captured, persisted.Status);
        Assert.Same(PaymentRail.Boleto, persisted.Rail);
        Assert.Equal("financeiro@fornecedor.com.br", persisted.Origin.SenderAddress);
    }

    // Os instrumentos sobrevivem à ida e volta do jsonb com banco, valor e vencimento intactos.
    // Regressão do formato: eles contêm VOs aninhados e são reidratados pelas factories do domínio.
    [Fact]
    public async Task PostImport_ShouldRoundTripTheInstrumentsThroughJsonb()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "financeiro@fornecedor.com.br"));

        var persisted = await ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .SingleAsync(b => b.Id == BillId.From(body.Id)));

        var instrument = Assert.Single(persisted.Instruments);
        Assert.Same(PaymentInstrumentKind.Barcode, instrument.Kind);
        Assert.Equal("341", instrument.DigitableLine.BankCode.Value);
        Assert.Equal(615.07m, instrument.DigitableLine.Amount.Amount);
        Assert.Equal(new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc), instrument.DigitableLine.DueDate);
    }

    // Havendo QR Pix, o trilho é Pix — ADR-010, decidido pelo agregado.
    [Fact]
    public async Task PostImport_WithBothInstruments_ShouldChooseThePixRail()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, DynamicPix, "Mailbox", ReceivedAt, SourceId, "financeiro@fornecedor.com.br"));

        Assert.Equal("Pix", body.Rail);

        var persisted = await ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .SingleAsync(b => b.Id == BillId.From(body.Id)));

        Assert.Equal(2, persisted.Instruments.Count);
    }

    // Arrecadação é reconhecida pelo código de barras, não informada por quem importa.
    [Fact]
    public async Task PostImport_WithUtilityBarcode_ShouldDeriveUtilityKind()
    {
        var body = await ImportAsync(new ImportBillRequest(
            UtilityLine, null, "Mailbox", ReceivedAt, SourceId, "cobranca@sabesp.com.br"));

        Assert.Equal("Utility", body.Kind);

        var bill = await Client.GetFromJsonAsync<BillContract>(
            new Uri($"{RouteFor(TenantId)}/{body.Id}", UriKind.Relative));

        // Arrecadação não carrega banco em posição nenhuma do código de barras.
        Assert.Null(bill!.BankCode);
        Assert.Equal(122.48m, bill.Amount);
    }

    // A captura anuncia o fato: o evento é gravado no outbox na MESMA transação do boleto.
    [Fact]
    public async Task PostImport_ShouldWriteTheCapturedEventToTheOutbox()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "financeiro@fornecedor.com.br"));

        var message = await ExecuteDbContextAsync(db => db.OutboxMessages.AsNoTracking().SingleAsync());

        Assert.Equal(typeof(BillCapturedDomainEvent).FullName, message.EventType);
        Assert.False(message.Processed);
        Assert.Contains(body.Id.ToString(), message.Payload, StringComparison.OrdinalIgnoreCase);
    }

    // O evento não carrega instrumento de pagamento — ele vai para o outbox e para o log.
    [Fact]
    public async Task OutboxPayload_ShouldNotContainAnyPaymentInstrument()
    {
        await ImportAsync(new ImportBillRequest(
            BankSlipLine, StaticPix, "Mailbox", ReceivedAt, SourceId, "financeiro@fornecedor.com.br"));

        var message = await ExecuteDbContextAsync(db => db.OutboxMessages.AsNoTracking().SingleAsync());

        Assert.DoesNotContain(BankSlipLine, message.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain(StaticPix, message.Payload, StringComparison.Ordinal);
    }

    // Importar o mesmo código de barras duas vezes no mesmo tenant é conflito — BLP.BIL02.
    [Fact]
    public async Task PostImport_WithTheSameBarcodeTwice_ShouldReturnConflict()
    {
        await ImportAsync(new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL02", error!.Id);
    }

    // A unicidade da chave de instrumento é GLOBAL: o mesmo boleto em outro tenant também
    // colide. Um compromisso é pago uma vez, e a caixa compartilhada torna isso provável.
    [Fact]
    public async Task PostImport_WithTheSameBarcodeInAnotherTenant_ShouldAlsoReturnConflict()
    {
        await ImportAsync(new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(OtherTenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, OtherSourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // A mensagem do conflito é genérica: não pode revelar de qual tenant é o boleto que colidiu.
    [Fact]
    public async Task PostImport_WhenCollidingAcrossTenants_ShouldNotRevealTheOtherTenant()
    {
        await ImportAsync(new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(OtherTenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, OtherSourceId, "a@b.com"));

        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(TenantId.ToString(), raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(BankSlipLine, raw, StringComparison.Ordinal);
    }

    // Boletos diferentes convivem — a colisão é por instrumento, não por tenant.
    [Fact]
    public async Task PostImport_WithDifferentBarcodes_ShouldBothBeAccepted()
    {
        await ImportAsync(new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));
        await ImportAsync(new ImportBillRequest(OtherBankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        Assert.Equal(2, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    // QR Pix estático é reutilizável: o mesmo payload no mês seguinte NÃO pode ser barrado.
    [Fact]
    public async Task PostImport_WithTheSameStaticPixTwice_ShouldBeAccepted()
    {
        await ImportAsync(new ImportBillRequest(null, StaticPix, "Mailbox", ReceivedAt, SourceId, "a@b.com"));
        await ImportAsync(new ImportBillRequest(null, StaticPix, "Mailbox", ReceivedAt.AddMonths(1), SourceId, "a@b.com"));

        Assert.Equal(2, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    // QR dinâmico nasce de uma cobrança específica e deduplica.
    [Fact]
    public async Task PostImport_WithTheSameDynamicPixTwice_ShouldReturnConflict()
    {
        await ImportAsync(new ImportBillRequest(null, DynamicPix, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(null, DynamicPix, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Linha digitável com DV quebrado não vira boleto — BLP.BIL01, e nada é gravado.
    [Fact]
    public async Task PostImport_WithTamperedLine_ShouldReturnBadRequestAndPersistNothing()
    {
        var tampered = string.Concat("3419123455", BankSlipLine.AsSpan(10));

        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(tampered, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL01", error!.Id);
        Assert.Equal(0, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    // Sem nenhum instrumento não existe boleto — BLP.BIL08.
    [Fact]
    public async Task PostImport_WithoutAnyInstrument_ShouldReturnBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(null, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL08", error!.Id);
    }

    // Origem de caixa de e-mail exige a fonte de captura que trouxe o documento — BLP.BIL11.
    [Fact]
    public async Task PostImport_FromMailboxWithoutSource_ShouldReturnBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            ImportRouteFor(TenantId),
            new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId: null, SenderAddress: "a@b.com"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL11", error!.Id);
    }

    // Upload manual não tem fonte cadastrada e mesmo assim é origem válida.
    [Fact]
    public async Task PostImport_FromManualUpload_ShouldBeAcceptedWithoutASource()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, null, "ManualUpload", ReceivedAt));

        var bill = await Client.GetFromJsonAsync<BillContract>(
            new Uri($"{RouteFor(TenantId)}/{body.Id}", UriKind.Relative));

        Assert.Equal("ManualUpload", bill!.Origin.SourceKind);
        Assert.Null(bill.Origin.SourceId);
    }

    // A leitura não devolve instrumento de pagamento — quem os tem, paga.
    [Fact]
    public async Task GetById_ShouldNeverExposeThePaymentInstruments()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, StaticPix, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.GetAsync(new Uri($"{RouteFor(TenantId)}/{body.Id}", UriKind.Relative));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(BankSlipLine, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(StaticPix, raw, StringComparison.Ordinal);
    }

    // Boleto de um tenant é invisível para outro.
    [Fact]
    public async Task GetById_FromAnotherTenant_ShouldReturnNotFound()
    {
        var body = await ImportAsync(new ImportBillRequest(
            BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));

        var response = await Client.GetAsync(new Uri($"{RouteFor(OtherTenantId)}/{body.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // A listagem só devolve os boletos do tenant da rota, do mais recente para o mais antigo.
    [Fact]
    public async Task GetList_ShouldReturnOnlyBillsOfTheRouteTenant()
    {
        await ImportAsync(new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));
        await ImportAsync(new ImportBillRequest(OtherBankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com"));
        await ImportAsync(
            new ImportBillRequest(UtilityLine, null, "Mailbox", ReceivedAt, OtherSourceId, "a@b.com"),
            OtherTenantId);

        var page = await Client.GetFromJsonAsync<BillPageContract>(RouteFor(TenantId));

        Assert.Equal(2, page!.Items.Count);
        Assert.All(page.Items, b => Assert.NotEqual("Utility", b.Kind));
    }

    // Repetir o mesmo x-requestid não importa duas vezes — idempotência do IdentifiedCommand.
    [Fact]
    public async Task PostImport_WithSameRequestId_ShouldImportOnlyOnce()
    {
        var requestId = new Guid("0195a1f0-0000-7000-8000-0000000000e1");
        var body = new ImportBillRequest(BankSlipLine, null, "Mailbox", ReceivedAt, SourceId, "a@b.com");

        var first = await SendWithRequestIdAsync(body, requestId);
        var second = await SendWithRequestIdAsync(body, requestId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var duplicate = await second.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        Assert.Equal(Guid.Empty, duplicate!.Id);
        Assert.Equal(1, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    private async Task<HttpResponseMessage> SendWithRequestIdAsync(ImportBillRequest body, Guid requestId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ImportRouteFor(TenantId))
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("x-requestid", requestId.ToString());

        return await Client.SendAsync(request);
    }

    private Task<Guid> SeedSourceAsync(Guid tenantId)
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Domain.SharedKernel.TenantId.From(tenantId),
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c9")),
                ReceivedAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id.Value;
        });

    private async Task<ImportBillResponseContract> ImportAsync(ImportBillRequest request, Guid? tenantId = null)
    {
        var response = await Client.PostAsJsonAsync(ImportRouteFor(tenantId ?? TenantId), request);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportBillResponseContract>())!;
    }
}
