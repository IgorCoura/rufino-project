namespace TenantManagement.Domain.Ports;

using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Leva a concessão de acesso até o provedor de identidade — o lugar de onde os produtos
/// leem, no token, a quais tenants a pessoa pertence e em quais deles cada produto vale.
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
    /// Garante que a pessoa exista no provedor e que ela enxergue este tenant nos produtos
    /// informados. Devolve o identificador dela — que pode ser a primeira vez que este BC fica
    /// sabendo dele.
    /// </summary>
    /// <param name="products">
    /// Os produtos ativos do tenant <strong>neste momento</strong>. A operação declara o estado
    /// desejado, não um incremento: produto ausente da lista tem o acesso <em>retirado</em>. É o
    /// que permite a mesma chamada servir para conceder acesso, ativar produto e desativar
    /// produto — sem que o provedor precise saber qual das três aconteceu.
    /// </param>
    /// <remarks>
    /// <strong>Não recebe nome de pessoa, e isso é deliberado.</strong> O vínculo é chaveado
    /// por e-mail justamente porque este BC não conhece quem está do outro lado — o cadastro
    /// que ele guarda é o do TENANT. Passar o nome do tenant como se fosse o da pessoa é o
    /// erro que já aconteceu: o titular apareceu no provedor chamando-se "Padaria do Zé LTDA".
    /// Quem informa o próprio nome é a pessoa, no primeiro acesso.
    /// </remarks>
    Task<AccessGrantResult> GrantAccessAsync(
        TenantId tenantId,
        string emailAddress,
        IReadOnlyCollection<ProductCode> products,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira o tenant da lista de acessos da pessoa, em todos os produtos. Não apaga a
    /// pessoa: ela pode ter acesso a outros tenants, e apagá-la seria decidir por eles.
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
