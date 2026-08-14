namespace TenantManagement.Infra.Identity;

using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// O que entra no lugar do adapter quando o provisionamento não está configurado.
/// </summary>
/// <remarks>
/// Falha alto, de propósito. Um dublê que fingisse sucesso deixaria o cadastro afirmando que
/// o acesso foi concedido quando ninguém consegue entrar — e a descoberta viria pelo cliente
/// ligando. Falhando, o vínculo fica marcado como falho, aparece na consulta, e o
/// reprovisionamento resolve assim que a configuração existir.
/// </remarks>
internal sealed class UnconfiguredTenantAccessProvisioner(ILogger<UnconfiguredTenantAccessProvisioner> logger)
    : ITenantAccessProvisioner
{
    public Task<AccessGrantResult> GrantAccessAsync(
        TenantId tenantId,
        string emailAddress,
        CancellationToken cancellationToken = default)
        => throw NotConfigured(tenantId);

    public Task RevokeAccessAsync(
        TenantId tenantId,
        string emailAddress,
        CancellationToken cancellationToken = default)
        => throw NotConfigured(tenantId);

    private InvalidOperationException NotConfigured(TenantId tenantId)
    {
        logger.LogWarning(
            "Provisionamento de acesso desligado: o vínculo do tenant {TenantId} fica pendente até {Section} ser configurado.",
            tenantId,
            TenantProvisioningOptions.SectionName);

        return new InvalidOperationException(
            $"Provisionamento de acesso não configurado. Preencha a seção '{TenantProvisioningOptions.SectionName}'.");
    }
}
