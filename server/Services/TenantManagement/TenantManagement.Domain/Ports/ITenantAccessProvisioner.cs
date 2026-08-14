namespace TenantManagement.Domain.Ports;

using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Leva a concessão de acesso até o provedor de identidade — o lugar de onde os produtos
/// leem, no token, a quais tenants a pessoa pertence.
/// </summary>
/// <remarks>
/// O domínio não sabe qual é o provedor, e nenhum termo dele (realm, claim, grupo, atributo)
/// atravessa esta porta. Trocar de provedor é um adapter novo e uma linha de configuração.
/// <para>
/// Toda operação é <strong>idempotente</strong>: conceder duas vezes concede uma, revogar o
/// que não existe não falha. É o que permite reprocessar sem medo quando algo ficou pendente.
/// </para>
/// </remarks>
public interface ITenantAccessProvisioner
{
    /// <summary>
    /// Garante que a pessoa exista no provedor e que ela enxergue este tenant. Devolve o
    /// identificador dela — que pode ser a primeira vez que este BC fica sabendo dele.
    /// </summary>
    Task<AccessGrantResult> GrantAccessAsync(
        TenantId tenantId,
        string emailAddress,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira o tenant da lista de acessos da pessoa. Não apaga a pessoa: ela pode ter
    /// acesso a outros tenants, e apagá-la seria decidir por eles.
    /// </summary>
    Task RevokeAccessAsync(
        TenantId tenantId,
        string emailAddress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// O que o provedor devolveu. <paramref name="UserWasCreated"/> distingue convidar alguém
/// novo de dar mais um acesso a quem já usa a plataforma — muda o que se diz à pessoa.
/// </summary>
public sealed record AccessGrantResult(UserId UserId, bool UserWasCreated);
