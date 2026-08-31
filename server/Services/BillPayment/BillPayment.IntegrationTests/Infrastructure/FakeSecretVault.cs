namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Cofre determinístico para os testes de adapter: resolve qualquer ponteiro para o segredo
/// fixado. As escritas não são suportadas — quem as exercita é <c>EnvelopeSecretVaultTests</c>,
/// contra o Postgres real.
/// </summary>
internal sealed class FakeSecretVault(string secret) : ISecretVault
{
    public Task<string> ResolveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
        => Task.FromResult(secret);

    public Task<CredentialRef> StoreAsync(
        TenantId tenantId, SecretKind kind, string secret, CancellationToken cancellationToken)
        => throw new NotSupportedException("Escrita não faz parte deste dublê.");

    public Task ReplaceAsync(CredentialRef credentialRef, string secret, CancellationToken cancellationToken)
        => throw new NotSupportedException("Escrita não faz parte deste dublê.");

    public Task RemoveAsync(CredentialRef credentialRef, CancellationToken cancellationToken)
        => throw new NotSupportedException("Escrita não faz parte deste dublê.");
}
