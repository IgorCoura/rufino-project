namespace BillPayment.Domain.Retention;

using BillPayment.Domain.SharedKernel;

/// <summary>Porta de acesso à política de retenção — uma por tenant.</summary>
public interface ICaptureRetentionPolicyRepository
{
    Task AddAsync(CaptureRetentionPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>. Devolve <c>null</c> quando o tenant nunca configurou.</summary>
    Task<CaptureRetentionPolicy?> GetAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>As políticas ligadas, para o worker de purga varrer.</summary>
    Task<IReadOnlyList<CaptureRetentionPolicy>> ListEnabledAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
