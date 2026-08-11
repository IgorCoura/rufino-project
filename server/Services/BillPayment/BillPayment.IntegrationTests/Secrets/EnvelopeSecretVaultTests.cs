namespace BillPayment.IntegrationTests.Secrets;

using System.Text;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Cofre de segredos por tenant contra o Postgres real: envelope encryption, vínculo do dado
/// autenticado com a linha, e a transacionalidade com quem chama.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class EnvelopeSecretVaultTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId OtherTenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));

    private const string ApiKey = "$aact_prod_000MzkwODA2MWY2OGM3MWRlMDU2NWM3MzJlNzZmNGZhZGY6OjAwMA==";

    // Ida e volta pelo banco: o que foi guardado é o que volta, decifrado por um escopo novo.
    [Fact]
    public async Task StoreAndResolve_AcrossScopes_ShouldReturnTheOriginalSecret()
    {
        var reference = await StoreAsync(Tenant, SecretKind.AsaasAccountApiKey, ApiKey);

        var resolved = await ResolveAsync(reference);

        Assert.Equal(ApiKey, resolved);
        Assert.True(reference.IsLocalVault);
    }

    // Exigência do ADR-009: dois Encrypt do mesmo payload produzem ciphertexts diferentes.
    // É a prova de que o nonce não é fixo — reusá-lo quebraria o AES-GCM.
    [Fact]
    public async Task StoreAsync_TwiceWithTheSamePayload_ShouldProduceDifferentCiphertexts()
    {
        var first = await StoreAsync(Tenant, SecretKind.AsaasAccountApiKey, ApiKey);
        var second = await StoreAsync(Tenant, SecretKind.AsaasAccountApiKey, ApiKey);

        var firstRow = await ReadRowAsync(first);
        var secondRow = await ReadRowAsync(second);

        Assert.NotEqual(firstRow.Ciphertext, secondRow.Ciphertext);
        Assert.NotEqual(firstRow.Nonce, secondRow.Nonce);
        Assert.NotEqual(firstRow.WrappedDek, secondRow.WrappedDek);
    }

    // O segredo não aparece em claro em nenhuma coluna da linha.
    [Fact]
    public async Task StoreAsync_ShouldNotPersistThePlaintextAnywhereInTheRow()
    {
        var reference = await StoreAsync(Tenant, SecretKind.PdfPassword, "52998224725");

        var row = await ReadRowAsync(reference);

        Assert.DoesNotContain("52998224725", Encoding.UTF8.GetString(row.Ciphertext), StringComparison.Ordinal);
        Assert.DoesNotContain("52998224725", Convert.ToBase64String(row.Ciphertext), StringComparison.Ordinal);
    }

    // O cofre não commita: guardar a credencial e criar o agregado que a referencia acontecem
    // na mesma transação de quem chamou. Sem isso, uma falha no meio deixaria credencial órfã.
    [Fact]
    public async Task StoreAsync_WithoutSaving_ShouldNotPersistAnything()
    {
        using var scope = Factory.Services.CreateScope();
        var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();

        var reference = await vault.StoreAsync(Tenant, SecretKind.MailboxOAuthToken, ApiKey, CancellationToken.None);

        var persisted = await ExecuteDbContextAsync(db => db.TenantSecrets.AsNoTracking().CountAsync());
        Assert.Equal(0, persisted);

        // Na mesma unidade de trabalho, porém, a credencial já resolve — é o que permite ao
        // handler usá-la antes do commit.
        Assert.Equal(ApiKey, await vault.ResolveAsync(reference, CancellationToken.None));
    }

    // O tenant entra no dado autenticado da cifra: mover a linha para outro tenant faz a
    // decifragem falhar em vez de devolver o segredo — BLP.SEC04.
    [Fact]
    public async Task ResolveAsync_AfterTheRowIsMovedToAnotherTenant_ShouldThrow_BLP_SEC04()
    {
        var reference = await StoreAsync(Tenant, SecretKind.AsaasAccountApiKey, ApiKey);

        await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.tenant_secrets SET tenant_id = {0} WHERE id = {1}",
            OtherTenant.Value, reference.AsLocalVaultId()));

        var ex = await Assert.ThrowsAsync<DomainException>(() => ResolveAsync(reference));

        Assert.Equal("BLP.SEC04", ex.Id);
    }

    // A natureza do segredo também está no dado autenticado: reapresentar um token de caixa
    // como chave de subconta não decifra — BLP.SEC04.
    [Fact]
    public async Task ResolveAsync_AfterTheKindIsChanged_ShouldThrow_BLP_SEC04()
    {
        var reference = await StoreAsync(Tenant, SecretKind.MailboxOAuthToken, ApiKey);

        await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.tenant_secrets SET kind = {0} WHERE id = {1}",
            SecretKind.AsaasAccountApiKey.Id, reference.AsLocalVaultId()));

        var ex = await Assert.ThrowsAsync<DomainException>(() => ResolveAsync(reference));

        Assert.Equal("BLP.SEC04", ex.Id);
    }

    // Texto cifrado adulterado é detectado pela etiqueta de autenticação — BLP.SEC04.
    [Fact]
    public async Task ResolveAsync_AfterTheCiphertextIsTampered_ShouldThrow_BLP_SEC04()
    {
        var reference = await StoreAsync(Tenant, SecretKind.PortalCredential, ApiKey);

        await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.tenant_secrets SET ciphertext = decode('00', 'hex') || ciphertext WHERE id = {0}",
            reference.AsLocalVaultId()));

        var ex = await Assert.ThrowsAsync<DomainException>(() => ResolveAsync(reference));

        Assert.Equal("BLP.SEC04", ex.Id);
    }

    // Referência que não existe mais no cofre é ausência, não falha de cifra — BLP.SEC03.
    [Fact]
    public async Task ResolveAsync_WithAnUnknownReference_ShouldThrow_BLP_SEC03()
    {
        var reference = CredentialRef.ForLocalVault(new Guid("0198a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => ResolveAsync(reference));

        Assert.Equal("BLP.SEC03", ex.Id);
    }

    // Refresh de token OAuth troca o valor mantendo a referência — o agregado que aponta para
    // ela não precisa ser mutado a cada renovação.
    [Fact]
    public async Task ReplaceAsync_ShouldChangeTheValueAndKeepTheReference()
    {
        var reference = await StoreAsync(Tenant, SecretKind.MailboxOAuthToken, "token-antigo");
        var before = await ReadRowAsync(reference);

        using (var scope = Factory.Services.CreateScope())
        {
            var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();
            var db = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();

            await vault.ReplaceAsync(reference, "token-novo", CancellationToken.None);
            await db.SaveEntitiesAsync();
        }

        var after = await ReadRowAsync(reference);

        Assert.Equal("token-novo", await ResolveAsync(reference));
        Assert.NotEqual(before.Nonce, after.Nonce);
        Assert.True(after.UpdatedAt >= before.UpdatedAt);
    }

    // Remover apaga a credencial; resolver depois é ausência.
    [Fact]
    public async Task RemoveAsync_ShouldDeleteTheCredential()
    {
        var reference = await StoreAsync(Tenant, SecretKind.PortalCredential, ApiKey);

        using (var scope = Factory.Services.CreateScope())
        {
            var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();
            var db = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();

            await vault.RemoveAsync(reference, CancellationToken.None);
            await db.SaveEntitiesAsync();
        }

        var ex = await Assert.ThrowsAsync<DomainException>(() => ResolveAsync(reference));
        Assert.Equal("BLP.SEC03", ex.Id);
    }

    // Remover o que já não existe é idempotente — a operação descreve um fim de estado.
    [Fact]
    public async Task RemoveAsync_WithAnUnknownReference_ShouldNotThrow()
    {
        using var scope = Factory.Services.CreateScope();
        var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();

        var reference = CredentialRef.ForLocalVault(new Guid("0198a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a99"));

        await vault.RemoveAsync(reference, CancellationToken.None);
    }

    // Credencial vazia não é credencial — BLP.SEC05.
    [Fact]
    public async Task StoreAsync_WithAnEmptySecret_ShouldThrow_BLP_SEC05()
    {
        using var scope = Factory.Services.CreateScope();
        var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => vault.StoreAsync(Tenant, SecretKind.PdfPassword, string.Empty, CancellationToken.None));

        Assert.Equal("BLP.SEC05", ex.Id);
    }

    // A versão da chave é gravada desde a primeira linha, mesmo havendo só uma versão:
    // acrescentá-la depois exigiria adivinhar com qual chave cada linha antiga foi cifrada.
    [Fact]
    public async Task StoreAsync_ShouldRecordTheKekVersionOnTheRow()
    {
        var reference = await StoreAsync(Tenant, SecretKind.AsaasAccountApiKey, ApiKey);

        var row = await ReadRowAsync(reference);

        Assert.Equal(1, row.KekVersion);
        Assert.Equal(Tenant.Value, row.TenantId);
        Assert.Equal(SecretKind.AsaasAccountApiKey.Id, row.Kind);
    }

    private async Task<CredentialRef> StoreAsync(TenantId tenantId, SecretKind kind, string secret)
    {
        using var scope = Factory.Services.CreateScope();
        var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();
        var db = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();

        var reference = await vault.StoreAsync(tenantId, kind, secret, CancellationToken.None);
        await db.SaveEntitiesAsync();

        return reference;
    }

    private async Task<string> ResolveAsync(CredentialRef reference)
    {
        using var scope = Factory.Services.CreateScope();
        var vault = scope.ServiceProvider.GetRequiredService<ISecretVault>();

        return await vault.ResolveAsync(reference, CancellationToken.None);
    }

    private async Task<TenantSecret> ReadRowAsync(CredentialRef reference)
        => await ExecuteDbContextAsync(db => db.TenantSecrets
            .AsNoTracking()
            .SingleAsync(s => s.Id == reference.AsLocalVaultId()));
}
