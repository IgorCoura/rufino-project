namespace BillPayment.IntegrationTests.CaptureSources;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Repositories;
using BillPayment.IntegrationTests.Infrastructure;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureSourcePersistenceTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly DateTime OccurredAt = new(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);

    private const string SharedMailbox = "contas@empresa.com.br";

    public CaptureSourcePersistenceTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Uma fonte conectada sobrevive ao round-trip com o ponteiro do cofre intacto — e a coluna
    // guarda a referência, nunca o segredo.
    [Fact]
    public async Task Connect_ShouldRoundTripAllFieldsIncludingCredentialRef()
    {
        var credential = CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));
        var id = await AddSourceAsync(TenantA, SharedMailbox, credential);

        var stored = await ExecuteDbContextAsync(db => db.CaptureSources
            .AsNoTracking()
            .FirstAsync(s => s.Id == id));

        Assert.Equal(SharedMailbox, stored.Address);
        Assert.Same(CaptureSourceKind.MicrosoftGraphMailbox, stored.Kind);
        Assert.Equal(credential, stored.Credential);
        Assert.True(stored.IsEnabled);
        Assert.Null(stored.SyncCursor);
        Assert.Null(stored.LastSyncAt);
    }

    // O cursor e o erro de sincronização sobrevivem ao round-trip — é deles que depende retomar
    // a caixa de onde parou.
    [Fact]
    public async Task RecordSync_ShouldPersistCursorAndError()
    {
        var id = await AddSourceAsync(TenantA, SharedMailbox);

        await ExecuteDbContextAsync(async db =>
        {
            var source = await db.CaptureSources.FirstAsync(s => s.Id == id);
            source.RecordSyncSuccess("deltaLink-abc", OccurredAt.AddMinutes(1));
            await db.SaveEntitiesAsync();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var source = await db.CaptureSources.FirstAsync(s => s.Id == id);
            source.RecordSyncFailure("503 Service Unavailable", OccurredAt.AddMinutes(2));
            await db.SaveEntitiesAsync();
        });

        var stored = await ExecuteDbContextAsync(db => db.CaptureSources
            .AsNoTracking()
            .FirstAsync(s => s.Id == id));

        // A falha não pode ter mexido no cursor: avançá-lo pularia mensagens, apagá-lo varreria a caixa.
        Assert.Equal("deltaLink-abc", stored.SyncCursor);
        Assert.Equal("503 Service Unavailable", stored.LastSyncError);
    }

    // A mesma conta não conecta a mesma caixa duas vezes — o índice único é quem garante sob concorrência.
    [Fact]
    public async Task Connect_SameAddressTwiceInSameTenant_ShouldViolateUniqueIndex()
    {
        await AddSourceAsync(TenantA, SharedMailbox);

        await Assert.ThrowsAsync<UniqueConstraintException>(
            () => AddSourceAsync(TenantA, SharedMailbox));
    }

    // DUAS contas monitorando a MESMA caixa é o caso central do ADR-008, não um erro: o índice
    // global sobre o endereço é deliberadamente NÃO único.
    [Fact]
    public async Task Connect_SameAddressInDifferentTenants_ShouldBothPersist()
    {
        var idA = await AddSourceAsync(TenantA, SharedMailbox);
        var idB = await AddSourceAsync(TenantB, SharedMailbox);

        var total = await ExecuteDbContextAsync(db => db.CaptureSources
            .AsNoTracking()
            .CountAsync(s => s.Address == SharedMailbox));

        Assert.Equal(2, total);
        Assert.NotEqual(idA, idB);
    }

    // A travessia autorizada nº 1 avisa que outra conta já monitora a caixa — devolvendo bool,
    // sem identificar quem.
    [Fact]
    public async Task IsAddressMonitoredByAnyTenant_WhenAnotherTenantMonitors_ShouldReturnTrue()
    {
        await AddSourceAsync(TenantB, SharedMailbox);

        var monitored = await ExecuteRepositoryAsync(repo =>
            repo.IsAddressMonitoredByAnyTenantAsync(SharedMailbox, TenantA));

        Assert.True(monitored);
    }

    // A própria fonte do tenant não faz o aviso disparar — senão toda conexão avisaria a si mesma.
    [Fact]
    public async Task IsAddressMonitoredByAnyTenant_WhenOnlyOwnSourceExists_ShouldReturnFalse()
    {
        await AddSourceAsync(TenantA, SharedMailbox);

        var monitored = await ExecuteRepositoryAsync(repo =>
            repo.IsAddressMonitoredByAnyTenantAsync(SharedMailbox, TenantA));

        Assert.False(monitored);
    }

    // A varredura do worker traz as fontes habilitadas de toda a instalação, com a nunca
    // sincronizada na frente — senão uma caixa recém-conectada ficaria atrás da fila para sempre.
    [Fact]
    public async Task ListEnabledForWorker_ShouldSkipDisabledAndPrioritizeNeverSynced()
    {
        var sincronizadaId = await AddSourceAsync(TenantA, "ja-sincronizada@empresa.com.br");
        var novaId = await AddSourceAsync(TenantB, "recem-conectada@empresa.com.br");
        var desligadaId = await AddSourceAsync(TenantA, "desligada@empresa.com.br");

        await ExecuteDbContextAsync(async db =>
        {
            var sincronizada = await db.CaptureSources.FirstAsync(s => s.Id == sincronizadaId);
            sincronizada.RecordSyncSuccess("cursor", OccurredAt.AddMinutes(1));

            var desligada = await db.CaptureSources.FirstAsync(s => s.Id == desligadaId);
            desligada.SetEnabled(false, OccurredAt.AddMinutes(1));

            await db.SaveEntitiesAsync();
        });

        var devidas = await ExecuteRepositoryAsync(repo => repo.ListEnabledForWorkerAsync(10));

        Assert.Equal(2, devidas.Count);
        Assert.Equal(novaId, devidas[0].Id);
        Assert.DoesNotContain(devidas, s => s.Id == desligadaId);
    }

    private Task<CaptureSourceId> AddSourceAsync(TenantId tenantId, string address, CredentialRef? credential = null)
        => ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                tenantId,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa de contas a pagar",
                address,
                credential ?? CredentialRef.ForLocalVault(Guid.CreateVersion7()),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

    private Task<T> ExecuteRepositoryAsync<T>(Func<ICaptureSourceRepository, Task<T>> action)
        => ExecuteDbContextAsync(db => action(new CaptureSourceRepository(db)));
}
