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
    /// <summary>
    /// Busca o retrato com TODAS as permissões que o token concede — uma pergunta por token, não
    /// por endpoint.
    /// </summary>
    /// <remarks>
    /// É a chamada que sustenta o <see cref="IRptCache"/>. Vai ao endpoint de token com
    /// <c>response_mode=permissions</c> e <strong>sem</strong> o parâmetro <c>permission</c>: sem
    /// ele o Keycloak avalia tudo que a pessoa alcança naquele resource server e devolve a lista.
    /// </remarks>
    Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default);
}
