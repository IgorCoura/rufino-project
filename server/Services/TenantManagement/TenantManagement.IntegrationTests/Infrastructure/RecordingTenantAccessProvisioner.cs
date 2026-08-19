namespace TenantManagement.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Provedor de identidade em memória, programável.
/// </summary>
/// <remarks>
/// Não é mock de comportamento: ele registra o que recebeu e devolve o que foi programado.
/// Nenhum teste asserta "foi chamado" — quem prova a orquestração é o estado do vínculo no
/// banco, que é o que o usuário enxerga.
/// </remarks>
public sealed class RecordingTenantAccessProvisioner : ITenantAccessProvisioner
{
    private readonly ConcurrentBag<(TenantId TenantId, string Email, IReadOnlyCollection<ProductCode> Products)> _granted = [];
    private readonly ConcurrentBag<(TenantId TenantId, string Email)> _revoked = [];

    /// <summary>Quando ligado, toda concessão falha — é o caminho do vínculo que fica pendente.</summary>
    public bool FailGrants { get; set; }

    public bool FailRevocations { get; set; }

    /// <summary>
    /// O que foi concedido, com os produtos que acompanharam. Os produtos importam: a mesma
    /// chamada serve para conceder acesso, ativar e desativar produto, e é a lista que distingue
    /// as três.
    /// </summary>
    public IReadOnlyCollection<(TenantId TenantId, string Email, IReadOnlyCollection<ProductCode> Products)> Granted => _granted.ToList();

    public IReadOnlyCollection<(TenantId TenantId, string Email)> Revoked => _revoked.ToList();

    /// <summary>Identificador estável por e-mail: o mesmo endereço devolve sempre o mesmo id.</summary>
    public static UserId UserIdFor(string email)
    {
        var bytes = new byte[16];
        var source = System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant());
        Array.Copy(source, bytes, Math.Min(source.Length, bytes.Length));
        return UserId.From(new Guid(bytes));
    }

    public Task<AccessGrantResult> GrantAccessAsync(
        TenantId tenantId,
        string emailAddress,
        IReadOnlyCollection<ProductCode> products,
        CancellationToken cancellationToken = default)
    {
        if (FailGrants)
            throw new InvalidOperationException("Falha programada no provedor de identidade.");

        _granted.Add((tenantId, emailAddress, products));
        return Task.FromResult(new AccessGrantResult(UserIdFor(emailAddress), UserWasCreated: true));
    }

    public Task RevokeAccessAsync(TenantId tenantId, string emailAddress, CancellationToken cancellationToken = default)
    {
        if (FailRevocations)
            throw new InvalidOperationException("Falha programada no provedor de identidade.");

        _revoked.Add((tenantId, emailAddress));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        FailGrants = false;
        FailRevocations = false;
        _granted.Clear();
        _revoked.Clear();
    }
}
