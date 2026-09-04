namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Repositories;
using BillPayment.IntegrationTests.Infrastructure;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureItemPersistenceTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly CaptureSourceId SourceA = CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b1"));
    private static readonly CaptureSourceId SourceB = CaptureSourceId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b2"));
    private static readonly DateTime OccurredAt = new(2026, 8, 10, 9, 30, 0, DateTimeKind.Utc);

    private const string MessageId = "AAMkAGI2THVSAAA=";

    public CaptureItemPersistenceTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Um item ingerido sobrevive ao round-trip, e os Smart Enums opcionais voltam nulos enquanto
    // o roteamento e a extração não aconteceram.
    [Fact]
    public async Task Ingest_ShouldRoundTripWithNullOptionalEnums()
    {
        var id = await AddItemAsync(TenantA, SourceA, MessageId, "boleto-enel.pdf");

        var stored = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .FirstAsync(i => i.Id == id));

        Assert.Same(CaptureItemStatus.Received, stored.Status);
        Assert.Equal("faturas@enel.com.br", stored.Sender);
        Assert.Null(stored.Routing);
        Assert.Null(stored.Extraction);
        Assert.Null(stored.BillId);
        Assert.Null(stored.ClaimedBy);
    }

    // Promover grava o boleto gerado e o degrau de roteamento — os dois Smart Enums opcionais
    // preenchidos, que é onde uma conversão nullable mal escrita quebraria.
    [Fact]
    public async Task Promote_ShouldPersistBillAndRoutingConfidence()
    {
        var id = await AddItemAsync(TenantA, SourceA, MessageId, "boleto-enel.pdf");
        var billId = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d1"));

        await ExecuteDbContextAsync(async db =>
        {
            var item = await db.CaptureItems.FirstAsync(i => i.Id == id);
            item.StoreArtifact("sha256:9f2c4a1b", "tenants/a/capture/boleto-enel.pdf", OccurredAt);
            item.MarkParsed(ExtractionMethod.QrCode, "cnpj_first_5", OccurredAt);
            item.Promote(billId, RoutingConfidence.Strong, OccurredAt);
            await db.SaveEntitiesAsync();
        });

        var stored = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .FirstAsync(i => i.Id == id));

        Assert.Same(CaptureItemStatus.Promoted, stored.Status);
        Assert.Equal(billId, stored.BillId);
        Assert.Same(RoutingConfidence.Strong, stored.Routing);
        Assert.Same(ExtractionMethod.QrCode, stored.Extraction);

        // Guarda QUAL campo derivou a senha, jamais a senha (ADR-009).
        Assert.Equal("cnpj_first_5", stored.UnlockedBy);
    }

    // Reprocessar a caixa não pode gerar item novo: o mesmo artefato da mesma mensagem colide.
    [Fact]
    public async Task Ingest_SameMessageAndArtifactTwice_ShouldViolateUniqueIndex()
    {
        await AddItemAsync(TenantA, SourceA, MessageId, "boleto-enel.pdf");

        await Assert.ThrowsAsync<UniqueConstraintException>(
            () => AddItemAsync(TenantA, SourceA, MessageId, "boleto-enel.pdf"));
    }

    // Um e-mail com três boletos gera três itens — é a chave por artefato que torna isso possível.
    [Fact]
    public async Task Ingest_SameMessageDifferentArtifacts_ShouldAllPersist()
    {
        await AddItemAsync(TenantA, SourceA, MessageId, "boleto-1.pdf");
        await AddItemAsync(TenantA, SourceA, MessageId, "boleto-2.pdf");
        await AddItemAsync(TenantA, SourceA, MessageId, "boleto-3.pdf");

        var total = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .CountAsync(i => i.ExternalMessageId == MessageId));

        Assert.Equal(3, total);
    }

    // A mesma mensagem lida por duas fontes de dois tenants gera DOIS itens, e isso é correto
    // (ADR-008) — cada pipeline enxerga só o seu.
    [Fact]
    public async Task Ingest_SameMessageInTwoTenants_ShouldProduceIndependentItems()
    {
        await AddItemAsync(TenantA, SourceA, MessageId, "boleto-enel.pdf");
        await AddItemAsync(TenantB, SourceB, MessageId, "boleto-enel.pdf");

        var doTenantA = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .CountAsync(i => i.TenantId == TenantA));

        var total = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .CountAsync(i => i.ExternalMessageId == MessageId));

        Assert.Equal(1, doTenantA);
        Assert.Equal(2, total);
    }

    // A dedup por conteúdo acha o item mais antigo com o mesmo hash — o mesmo boleto reenviado
    // noutra thread tem outro id de mensagem.
    [Fact]
    public async Task FindOriginalByContentHash_ShouldReturnOldestNonDiscardedItem()
    {
        const string hash = "sha256:9f2c4a1b";
        var originalId = await AddItemAsync(TenantA, SourceA, "msg-1", "boleto.pdf", hash);
        var reenvioId = await AddItemAsync(TenantA, SourceA, "msg-2", "boleto.pdf", hash);

        var encontrado = await ExecuteRepositoryAsync(repo =>
            repo.FindOriginalByContentHashAsync(TenantA, hash, reenvioId));

        Assert.Equal(originalId, encontrado);
    }

    // A dedup não enxerga o hash de outro tenant — o isolamento vale aqui como em toda busca.
    [Fact]
    public async Task FindOriginalByContentHash_ShouldNotSeeOtherTenantItems()
    {
        const string hash = "sha256:9f2c4a1b";
        await AddItemAsync(TenantB, SourceB, "msg-1", "boleto.pdf", hash);
        var doTenantA = await AddItemAsync(TenantA, SourceA, "msg-2", "boleto.pdf", hash);

        var encontrado = await ExecuteRepositoryAsync(repo =>
            repo.FindOriginalByContentHashAsync(TenantA, hash, doTenantA));

        Assert.Null(encontrado);
    }

    // Descartar guarda o ponteiro para o original, e o descartado deixa de servir de original —
    // senão as duplicatas se encadeariam e a trilha não levaria ao artefato de verdade.
    [Fact]
    public async Task Discard_ShouldPersistPointerAndStopBeingAnOriginal()
    {
        const string hash = "sha256:9f2c4a1b";
        var originalId = await AddItemAsync(TenantA, SourceA, "msg-1", "boleto.pdf", hash);
        var primeiroReenvioId = await AddItemAsync(TenantA, SourceA, "msg-2", "boleto.pdf", hash);

        await ExecuteDbContextAsync(async db =>
        {
            var reenvio = await db.CaptureItems.FirstAsync(i => i.Id == primeiroReenvioId);
            reenvio.Discard(originalId, OccurredAt);
            await db.SaveEntitiesAsync();
        });

        var segundoReenvioId = await AddItemAsync(TenantA, SourceA, "msg-3", "boleto.pdf", hash);
        var encontrado = await ExecuteRepositoryAsync(repo =>
            repo.FindOriginalByContentHashAsync(TenantA, hash, segundoReenvioId));

        Assert.Equal(originalId, encontrado);

        var descartado = await ExecuteDbContextAsync(db => db.CaptureItems
            .AsNoTracking()
            .FirstAsync(i => i.Id == primeiroReenvioId));

        Assert.Equal(originalId, descartado.DiscardedOf);
    }

    private Task<CaptureItemId> AddItemAsync(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string artifactKey,
        string? contentHash = null)
        => ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                tenantId,
                sourceId,
                externalMessageId,
                artifactKey,
                "faturas@enel.com.br",
                "Sua fatura de energia chegou",
                OccurredAt.AddHours(-1),
                OccurredAt);

            if (contentHash is not null)
                item.StoreArtifact(contentHash, $"tenants/capture/{artifactKey}", OccurredAt);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });

    private Task<T> ExecuteRepositoryAsync<T>(Func<ICaptureItemRepository, Task<T>> action)
        => ExecuteDbContextAsync(db => action(new CaptureItemRepository(db)));
}
