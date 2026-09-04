namespace BillPayment.IntegrationTests.CaptureItems;

using System.Net;
using System.Text;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Extraction;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;

/// <summary>
/// Quem pode abrir o documento original de um item da quarentena.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O endpoint existe para que reivindicar deixe de ser decisão no escuro.</strong> Sem
/// ver o papel, a pessoa confirma que a conta é dela olhando remetente e assunto — que é
/// exatamente o que o roteamento automático já não conseguiu usar para decidir.
/// </para>
/// <para>
/// O gate é o mesmo do DTO (<c>ExposesFinancialDetail</c>, ADR-008), e é aqui que ele vale mais:
/// vazar o <em>documento</em> de outro pagador é pior que vazar o ponteiro dele.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureItemArtifactTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(TestTenants.Primary);
    private static readonly TenantId TenantB = TenantId.From(TestTenants.Secondary);
    private static readonly CaptureSourceId Source =
        CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b7"));
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 9, 0, 0, DateTimeKind.Utc);

    private const string SourceUrl = "https://fatura.enel.com.br/2via/abc123";
    private const string ContentHash = "sha256:9f2c4a1b";
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("%PDF-1.4 boleto de teste");

    private readonly WebApplicationFactory<Program> _host;
    private readonly InMemoryAttachmentStorage _storage;

    public CaptureItemArtifactTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        // O host irmão, e não a fábrica base: é ele que tem o armazenamento em memória. Na base,
        // o armazenamento não configurado falha em toda leitura — de propósito.
        _host = factory.WithCaptureChain();
        _storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
    }

    // O caso que justifica o endpoint: item sem dono definido devolve o documento, e é com ele
    // que a pessoa decide se reivindica.
    [Fact]
    public async Task GetArtifact_WhenUnrouted_ShouldServeTheOriginalDocument()
    {
        var id = await SeedWithArtifactAsync(item => item.MarkUnrouted("payer_not_identified", OccurredAt));

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Bytes, await response.Content.ReadAsByteArrayAsync());
    }

    // Item promovido também abre: é o mesmo documento que sustenta a decisão do aprovador.
    [Fact]
    public async Task GetArtifact_WhenPromoted_ShouldServeTheOriginalDocument()
    {
        var billId = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d7"));
        var id = await SeedWithArtifactAsync(item => item.Promote(billId, RoutingConfidence.Strong, OccurredAt));

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // O sistema SABE que o documento é de outro pagador. Entregá-lo seria vazamento gratuito —
    // e 404, não 403, porque distinguir já confirmaria que o item existe.
    [Fact]
    public async Task GetArtifact_WhenForeignPayer_ShouldReturnNotFound()
    {
        var id = await SeedWithArtifactAsync(item => item.MarkForeign("payer_is_another", OccurredAt));

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Item ainda no funil não abre: antes do roteamento ninguém sabe de quem é o documento, e
    // servir ali vazaria justamente na janela que antecede a descoberta de que o pagador é outro.
    [Fact]
    public async Task GetArtifact_WhileStillInPipeline_ShouldReturnNotFound()
    {
        var id = await SeedWithArtifactAsync(_ => { });

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Protegido por senha carrega sentinela no lugar da chave — existe item, não existe arquivo.
    // Sem HasStoredArtifact isto viraria uma busca por "pending-unlock" no balde, e um 500.
    [Fact]
    public async Task GetArtifact_WhenTheKeyIsASentinel_ShouldReturnNotFoundInsteadOfFailing()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var item = Ingest(TenantA);
            item.StoreArtifact(ContentHash, CaptureItem.PENDING_UNLOCK, OccurredAt);
            item.MarkLocked(OccurredAt);

            // Locked → Unrouted é transição válida, e é o que põe o item num estado que EXPÕE
            // detalhe carregando uma sentinela no lugar da chave. Sem passar por aqui, o gate de
            // visibilidade barraria antes e o teste não provaria nada sobre a sentinela.
            item.MarkUnrouted("payer_not_identified", OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Item do outro tenant não abre, mesmo com o cliente alcançando os dois — quem barra é o
    // filtro por TenantId da query, não o guard de rota.
    [Fact]
    public async Task GetArtifact_WhenTheItemBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var id = await SeedWithArtifactAsync(
            item => item.MarkUnrouted("payer_not_identified", OccurredAt),
            tenantId: TenantB);

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Chave registrada cujo objeto sumiu do balde: é falha de integridade do lado de lá, e para
    // quem pediu é "não há documento" — nunca um 500 na cara do usuário.
    [Fact]
    public async Task GetArtifact_WhenTheStoredObjectIsGone_ShouldReturnNotFound()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var item = Ingest(TenantA);
            item.StoreArtifact(ContentHash, $"tenants/{TenantA.Value:N}/captures/orfa.pdf", OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            item.MarkUnrouted("payer_not_identified", OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // O CASO QUE ESTE ENDPOINT PASSOU A RESOLVER: o emissor trancou o PDF com o documento do
    // pagador, o sistema derivou a senha na captura, e quem confere o boleto recebia mesmo assim
    // o arquivo trancado — sendo obrigado a digitar uma senha que o cadastro já tinha.
    [Fact]
    public async Task GetArtifact_WhenTheDocumentIsEncrypted_ShouldServeACopyThatOpensWithoutAPassword()
    {
        await SeedPayerProfileAsync();
        var id = await SeedWithArtifactAsync(
            item => item.MarkUnrouted("payer_not_identified", OccurredAt),
            content: EncryptedPdfFixture.Bytes());

        var response = await GetAsync(TestTenants.Primary, id);
        var served = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Abrir sem passar senha nenhuma é a prova. Os bytes também não são os guardados: o
        // original continua cifrado no balde, que é o comprovante do que o sistema viu.
        using var document = PdfDocument.Open(served);
        Assert.Equal(1, document.NumberOfPages);
        Assert.NotEqual(EncryptedPdfFixture.Bytes(), served);
    }

    // A contraprova: sem cadastro fiscal não há candidata, e o documento sai como está. O leitor
    // do app volta a pedir a senha — que é o certo, porque ali quem sabe algo é a pessoa.
    [Fact]
    public async Task GetArtifact_WhenTheTenantHasNoTaxRegistration_ShouldServeTheOriginalUntouched()
    {
        var id = await SeedWithArtifactAsync(
            item => item.MarkUnrouted("payer_not_identified", OccurredAt),
            content: EncryptedPdfFixture.Bytes());

        var response = await GetAsync(TestTenants.Primary, id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(EncryptedPdfFixture.Bytes(), await response.Content.ReadAsByteArrayAsync());
    }

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                TenantA,
                PayerKind.Company,
                "RUFINO EMPREITEIRA LTDA",
                TaxId.Parse(EncryptedPdfFixture.TenantCnpj),
                OccurredAt));

            await db.SaveEntitiesAsync();
        });

    private static CaptureItem Ingest(TenantId tenantId) => CaptureItem.Ingest(
        tenantId,
        Source,
        "AAMkAGI2THVSAAA=",
        "boleto-enel.pdf",
        "faturas@enel.com.br",
        "Sua fatura de energia chegou",
        OccurredAt.AddHours(-1),
        OccurredAt);

    private async Task<CaptureItemId> SeedWithArtifactAsync(
        Action<CaptureItem> arrange,
        TenantId? tenantId = null,
        byte[]? content = null)
    {
        var tenant = tenantId ?? TenantA;
        var key = await _storage.StoreAsync(
            tenant, "boleto-enel.pdf", "application/pdf", content ?? Bytes, default);

        return await ExecuteDbContextAsync(async db =>
        {
            var item = Ingest(tenant);
            item.MarkLinkPending(SourceUrl, OccurredAt);
            item.StoreArtifact(ContentHash, key, OccurredAt);
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, OccurredAt);
            arrange(item);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }

    private Task<HttpResponseMessage> GetAsync(Guid tenantId, CaptureItemId id)
        => _host.CreateClient().Authenticated().GetAsync(
            new Uri($"/api/v1/{tenantId}/capture-items/{id.Value}/artifact", UriKind.Relative));
}
