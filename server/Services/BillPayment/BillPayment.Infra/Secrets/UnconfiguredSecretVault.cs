namespace BillPayment.Infra.Secrets;

using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Registrado no lugar do cofre real quando não há master key configurada.
/// </summary>
/// <remarks>
/// <strong>Falha em todas as operações, inclusive nas de leitura.</strong> A alternativa —
/// guardar em claro, ou devolver vazio — trocaria uma falha barulhenta no primeiro uso por um
/// vazamento silencioso. A aplicação sobe sem master key porque a Fase 1 não guarda credencial
/// de tenant; a partir da fase 2 a chave é pré-requisito de deploy.
/// </remarks>
internal sealed class UnconfiguredSecretVault : ISecretVault
{
    public Task<string> ResolveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
        => throw SecretErrors.VaultNotConfigured();

    public Task<CredentialRef> StoreAsync(TenantId tenantId, SecretKind kind, string secret, CancellationToken cancellationToken)
        => throw SecretErrors.VaultNotConfigured();

    public Task ReplaceAsync(CredentialRef credentialRef, string secret, CancellationToken cancellationToken)
        => throw SecretErrors.VaultNotConfigured();

    public Task RemoveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
        => throw SecretErrors.VaultNotConfigured();
}
