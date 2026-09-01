namespace BillPayment.API.Authorization;

/// <summary>Desfecho de uma checagem de permissão UMA contra o servidor de autorização.</summary>
public enum ResourceAccessResult
{
    /// <summary>O token vale e concede o recurso/escopos pedidos.</summary>
    Granted,

    /// <summary>O token vale, mas não concede o recurso/escopos pedidos.</summary>
    Denied,

    /// <summary>O servidor de autorização recusou o próprio token (expirado, revogado, not-before).</summary>
    InvalidToken,

    /// <summary>O servidor de autorização não foi alcançado ou respondeu com erro de servidor.</summary>
    ServerUnavailable,
}

public interface IAuthorizationServerClient
{
    Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pergunta ao servidor de autorização QUAIS dos escopos pedidos o token concede sobre o
    /// recurso, e devolve o subconjunto concedido — é a base da alçada por nível de risco.
    /// Falha de rede ou recusa devolvem o conjunto vazio: negar por indisponibilidade é o lado
    /// seguro, e a porta de entrada do endpoint já validou o token e o escopo base.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetGrantedScopesAsync(
        string resource,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default);
}
