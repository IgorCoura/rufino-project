namespace TenantManagement.Application.Tenants.EventHandlers;

using Microsoft.Extensions.Logging;
using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;

/// <summary>
/// Reescreve no provedor de identidade o acesso de TODOS os vínculos do tenant, a partir do
/// estado atual do agregado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O estado desejado é derivado do agregado, nunca do evento.</strong> Suspender,
/// reativar, ativar produto e desativar produto mudam a mesma coisa — quem enxerga aquele tenant
/// e em quais produtos — e por isso são a mesma operação aqui. Ler o payload do evento faria cada
/// handler recalcular a resposta por conta própria, e três deles acertariam.
/// </para>
/// <para>
/// <strong>Tenant suspenso revoga, mesmo com o vínculo ativo.</strong> Suspender preserva o
/// cadastro e corta o acesso — é o que <c>TenantStatus.Suspended</c> declara. Por isso a
/// revogação passa pelo <em>provisionador</em>, e não por <c>RevokeMembership</c>: o método de
/// domínio protege o último responsável (<c>TNM.TNT20</c>) e recusaria cortar justamente o dono,
/// deixando a suspensão pela metade — e reativar exigiria recriar vínculos, perdendo papel e
/// histórico.
/// </para>
/// <para>
/// A falha do provedor é engolida por vínculo: cada um fica marcado como falho, visível na
/// consulta e curável por <c>POST /tenants/{id}/access/reprovision</c>. Uma caixa fora do ar não
/// pode impedir os outros vínculos de serem sincronizados.
/// </para>
/// </remarks>
public sealed class TenantAccessSynchronizer(
    ITenantRepository repository,
    ITenantAccessProvisioner provisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<TenantAccessSynchronizer> logger)
{
    public async Task SyncAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return;

        var occurredAt = timeProvider.GetUtcNow().UtcDateTime;
        var suspended = tenant.Status.Equals(TenantStatus.Suspended);
        var products = tenant.ActiveProducts;

        // Materializa os e-mails antes do laço: o corpo grava no agregado, e iterar a coleção
        // que está sendo mutada é como se descobre isso da pior forma.
        var emails = tenant.Memberships.Where(m => m.IsActive).Select(m => m.Email).ToList();

        foreach (var email in emails)
        {
            try
            {
                if (suspended)
                    await provisioner.RevokeAccessAsync(tenantId, email, cancellationToken);
                else
                    await provisioner.GrantAccessAsync(tenantId, email, products, cancellationToken);

                tenant.ConfirmAccessProvisioned(email, null, occurredAt);
            }
#pragma warning disable CA1031 // Qualquer falha do provedor é a mesma decisão: registrar e deixar curável.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(
                    ex,
                    "Falha ao sincronizar o acesso do tenant {TenantId} no provedor. O vínculo fica marcado como falho e pode ser reprocessado.",
                    tenantId);

                tenant.MarkAccessProvisioningFailed(email, occurredAt);
            }
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
