namespace EconomicCore.Domain.SeedWork;

/// <summary>
/// Porta de idempotência: registra IDs de requests já vistas (header
/// <c>x-requestid</c>) para impedir reprocessamento. Vive no SeedWork — como os
/// demais ports — porque a Infra não referencia a Application. Implementação na Infra.
/// </summary>
public interface IRequestManager
{
    Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agenda a inserção do request na tabela de idempotência <b>sem</b> commitar.
    /// O <c>SaveEntitiesAsync</c> do handler real persiste a marca junto com o
    /// efeito do comando — tudo na mesma transação do <c>DbContext</c> Scoped.
    /// </summary>
    Task CreateRequestForCommandAsync<TCommand>(Guid id, CancellationToken cancellationToken = default);
}
