namespace BillPayment.Application.Queries.Payees;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única
/// exceção autorizada a tocar a Infra, conforme registrado no CLAUDE.md do BC.
/// </summary>
public interface IPayeeQueries
{
    Task<PayeePage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PayeeDto?> GetAsync(
        Guid tenantId,
        Guid payeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca pelo documento do beneficiário, que é a chave usada pela verificação do boleto.
    /// Devolve <c>null</c> quando não há cadastro — o check então sai inconclusivo, não reprovado.
    /// </summary>
    Task<PayeeDto?> FindByTaxIdAsync(
        Guid tenantId,
        string taxId,
        CancellationToken cancellationToken = default);
}
