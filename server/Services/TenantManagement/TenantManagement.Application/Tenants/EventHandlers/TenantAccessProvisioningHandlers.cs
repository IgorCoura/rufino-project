namespace TenantManagement.Application.Tenants.EventHandlers;

using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Leva ao provedor de identidade o acesso que o cadastro acabou de conceder.
/// </summary>
/// <remarks>
/// Roda <strong>depois</strong> do commit, e é por isso que o resultado volta gravado no
/// agregado: o provedor não participa da transação do banco, então a única forma honesta de
/// representar "cadastrei mas não consegui dar acesso" é um estado persistido — não um log.
/// <para>
/// A falha é engolida de propósito. Derrubar a requisição faria o cliente reenviar um
/// cadastro que já existe; o que fica é o vínculo marcado como falho, visível na consulta e
/// curável por <c>POST /tenants/{id}/access/reprovision</c>.
/// </para>
/// </remarks>
public sealed class MembershipGrantedDomainEventHandler(
    ITenantRepository repository,
    ITenantAccessProvisioner provisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<MembershipGrantedDomainEventHandler> logger)
    : IDomainEventHandler<MembershipGrantedDomainEvent>
{
    public async Task HandleAsync(MembershipGrantedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var tenant = await repository.GetByIdAsync(domainEvent.TenantId, cancellationToken);
        if (tenant is null)
            return;

        var occurredAt = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var result = await provisioner.GrantAccessAsync(
                domainEvent.TenantId,
                domainEvent.Email,
                cancellationToken);

            tenant.ConfirmAccessProvisioned(domainEvent.Email, result.UserId, occurredAt);
        }
#pragma warning disable CA1031 // Qualquer falha do provedor é a mesma decisão: registrar e deixar curável.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(
                ex,
                "Falha ao provisionar acesso do tenant {TenantId}. O vínculo fica marcado como falho e pode ser reprocessado.",
                domainEvent.TenantId);

            tenant.MarkAccessProvisioningFailed(domainEvent.Email, occurredAt);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>Retira o tenant da lista de acessos de quem perdeu o vínculo.</summary>
public sealed class MembershipRevokedDomainEventHandler(
    ITenantRepository repository,
    ITenantAccessProvisioner provisioner,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<MembershipRevokedDomainEventHandler> logger)
    : IDomainEventHandler<MembershipRevokedDomainEvent>
{
    public async Task HandleAsync(MembershipRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var tenant = await repository.GetByIdAsync(domainEvent.TenantId, cancellationToken);
        if (tenant is null)
            return;

        var occurredAt = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            await provisioner.RevokeAccessAsync(domainEvent.TenantId, domainEvent.Email, cancellationToken);
            tenant.ConfirmAccessProvisioned(domainEvent.Email, null, occurredAt);
        }
#pragma warning disable CA1031 // Idem: a revogação pendente precisa ficar visível, não sumir num throw.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(
                ex,
                "Falha ao revogar acesso no tenant {TenantId}. A revogação fica pendente e pode ser reprocessada.",
                domainEvent.TenantId);

            tenant.MarkAccessProvisioningFailed(domainEvent.Email, occurredAt);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
