namespace BillPayment.Infra.Secrets;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cofre de segredos por tenant sobre a tabela <c>tenant_secrets</c>, com envelope encryption
/// em AES-256-GCM e master key vinda do ambiente (<c>ADR-009</c>).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nenhuma operação de escrita commita.</strong> Elas registram a mudança no
/// <c>DbContext</c> de quem chamou; o efeito só existe no <c>SaveEntitiesAsync</c> do handler.
/// É isso que torna atômico "guardar a credencial" e "criar o agregado que a referencia" —
/// sem isso, uma falha no meio deixaria credencial órfã no cofre ou agregado apontando para o
/// vazio. É também por isso que <see cref="ResolveAsync"/> usa <c>FindAsync</c>: ele enxerga a
/// linha recém-adicionada no rastreador antes de ela existir no banco.
/// </para>
/// <para>
/// <strong>Nonce novo a cada gravação, sem exceção.</strong> Reusar nonce em AES-GCM com a
/// mesma chave quebra a confidencialidade do esquema — não é boa prática, é requisito. O
/// <c>TenantSecret</c> não expõe setter por campo justamente para não haver caminho que
/// atualize o texto cifrado sem trocar o nonce.
/// </para>
/// <para>
/// <strong>Nada aqui entra em log.</strong> Nem o segredo, nem o DEK, nem a master key, nem a
/// referência resolvida.
/// </para>
/// </remarks>
internal sealed class EnvelopeSecretVault : ISecretVault
{
    private const int DEK_LENGTH = 32;
    private const int NONCE_LENGTH = 12;
    private const int TAG_LENGTH = 16;
    private const string AAD_PREFIX = "bpv1";

    private readonly BillPaymentDbContext _db;
    private readonly TimeProvider _clock;
    private readonly byte[] _masterKey;
    private readonly int _kekVersion;

    public EnvelopeSecretVault(BillPaymentDbContext db, TimeProvider clock, SecretsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _db = db;
        _clock = clock;
        _masterKey = options.ResolveMasterKey() ?? throw SecretErrors.VaultNotConfigured();
        _kekVersion = options.KekVersion;
    }

    public async Task<string> ResolveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
    {
        var row = await FindAsync(credentialRef, cancellationToken) ?? throw SecretErrors.SecretNotFound();

        var aad = BuildAad(row.TenantId, row.Kind, row.Id);
        var dek = Unwrap(row, aad);

        try
        {
            var plaintext = new byte[row.Ciphertext.Length];
            using var aes = new AesGcm(dek, TAG_LENGTH);
            aes.Decrypt(row.Nonce, row.Ciphertext, row.Tag, plaintext, aad);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            throw SecretErrors.SecretUnreadable();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public Task<CredentialRef> StoreAsync(
        TenantId tenantId,
        SecretKind kind,
        string secret,
        CancellationToken cancellationToken)
    {
        if (kind is null)
            throw SecretErrors.SecretKindRequired();
        if (string.IsNullOrEmpty(secret))
            throw SecretErrors.SecretValueRequired();

        cancellationToken.ThrowIfCancellationRequested();

        var now = _clock.GetUtcNow();
        var id = Guid.CreateVersion7();
        var row = new TenantSecret(id, tenantId.Value, kind.Id, _kekVersion, now);

        Seal(row, secret, now);
        _db.TenantSecrets.Add(row);

        return Task.FromResult(CredentialRef.ForLocalVault(id));
    }

    public async Task ReplaceAsync(CredentialRef credentialRef, string secret, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(secret))
            throw SecretErrors.SecretValueRequired();

        var row = await FindAsync(credentialRef, cancellationToken) ?? throw SecretErrors.SecretNotFound();

        Seal(row, secret, _clock.GetUtcNow());
    }

    public async Task RemoveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
    {
        var row = await FindAsync(credentialRef, cancellationToken);
        if (row is not null)
            _db.TenantSecrets.Remove(row);
    }

    private async Task<TenantSecret?> FindAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
    {
        if (credentialRef is null)
            throw SecretErrors.CredentialRefRequired();
        if (!credentialRef.IsLocalVault)
            throw SecretErrors.CredentialRefMalformed();

        // FindAsync consulta o rastreador antes do banco — é o que permite resolver uma
        // credencial guardada na mesma unidade de trabalho, antes do commit.
        return await _db.TenantSecrets.FindAsync([credentialRef.AsLocalVaultId()], cancellationToken);
    }

    private void Seal(TenantSecret row, string secret, DateTimeOffset at)
    {
        var aad = BuildAad(row.TenantId, row.Kind, row.Id);
        var dek = RandomNumberGenerator.GetBytes(DEK_LENGTH);

        try
        {
            var plaintext = Encoding.UTF8.GetBytes(secret);

            var nonce = RandomNumberGenerator.GetBytes(NONCE_LENGTH);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TAG_LENGTH];
            using (var payloadCipher = new AesGcm(dek, TAG_LENGTH))
                payloadCipher.Encrypt(nonce, plaintext, ciphertext, tag, aad);

            CryptographicOperations.ZeroMemory(plaintext);

            var dekNonce = RandomNumberGenerator.GetBytes(NONCE_LENGTH);
            var wrappedDek = new byte[dek.Length];
            var dekTag = new byte[TAG_LENGTH];
            using (var keyCipher = new AesGcm(_masterKey, TAG_LENGTH))
                keyCipher.Encrypt(dekNonce, dek, wrappedDek, dekTag, WrapAad(aad, _kekVersion));

            row.SetEnvelope(wrappedDek, dekNonce, dekTag, ciphertext, nonce, tag, _kekVersion, at);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    private byte[] Unwrap(TenantSecret row, byte[] aad)
    {
        try
        {
            var dek = new byte[row.WrappedDek.Length];
            using var cipher = new AesGcm(_masterKey, TAG_LENGTH);
            cipher.Decrypt(row.DekNonce, row.WrappedDek, row.DekTag, dek, WrapAad(aad, row.KekVersion));
            return dek;
        }
        catch (CryptographicException)
        {
            // Master key trocada, linha adulterada ou versão de chave divergente — de fora,
            // os três significam a mesma coisa: esta credencial não pode ser lida.
            throw SecretErrors.SecretUnreadable();
        }
    }

    // O tenant, o tipo e o id entram no dado autenticado: mover uma linha para outro tenant,
    // ou reapresentá-la como outro tipo de segredo, faz a decifragem falhar.
    private static byte[] BuildAad(Guid tenantId, int kind, Guid id)
        => Encoding.UTF8.GetBytes(string.Create(
            CultureInfo.InvariantCulture,
            $"{AAD_PREFIX}|{tenantId:N}|{kind}|{id:N}"));

    private static byte[] WrapAad(byte[] aad, int kekVersion)
        => [.. aad, .. Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"|kek={kekVersion}"))];
}
