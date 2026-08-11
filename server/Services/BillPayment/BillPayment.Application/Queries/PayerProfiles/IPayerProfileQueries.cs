namespace BillPayment.Application.Queries.PayerProfiles;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única
/// exceção autorizada a tocar a Infra, conforme registrado no CLAUDE.md do BC.
/// </summary>
public interface IPayerProfileQueries
{
    /// <summary>Devolve <c>null</c> quando o tenant ainda não se cadastrou — estado válido do onboarding.</summary>
    Task<PayerProfileDto?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
