namespace BillPayment.Application.Queries.Retention;

using BillPayment.Domain.Retention;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <param name="WindowDays">O prazo em vigor. Existe mesmo com a política desligada.</param>
/// <param name="AvailableWindowDays">
/// Os prazos que a tela oferece. Vêm do domínio para o cliente não manter a lista por conta
/// própria — uma faixa nova aparece na tela sem alterar o app.
/// </param>
public sealed record CaptureRetentionPolicyDto(
    bool IsEnabled,
    int WindowDays,
    IReadOnlyList<int> AvailableWindowDays);

/// <summary>Leitura da janela de retenção do livro-caixa.</summary>
public interface ICaptureRetentionQueries
{
    /// <summary>
    /// A política do tenant. <strong>Nunca devolve nulo</strong>: quem nunca configurou recebe o
    /// padrão — desligada, com a janela padrão pré-escolhida —, senão a tela teria de inventar um
    /// estado inicial e ele divergiria do que o domínio criaria depois.
    /// </summary>
    Task<CaptureRetentionPolicyDto> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

internal sealed class CaptureRetentionQueries(BillPaymentDbContext context) : ICaptureRetentionQueries
{
    public async Task<CaptureRetentionPolicyDto> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);

        var policy = await context.CaptureRetentionPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant, cancellationToken);

        var available = Enumeration.GetAll<RetentionWindow>()
            .OrderBy(w => w.Days)
            .Select(w => w.Days)
            .ToList();

        return policy is null
            ? new CaptureRetentionPolicyDto(false, RetentionWindow.Default.Days, available)
            : new CaptureRetentionPolicyDto(policy.IsEnabled, policy.Window.Days, available);
    }
}
