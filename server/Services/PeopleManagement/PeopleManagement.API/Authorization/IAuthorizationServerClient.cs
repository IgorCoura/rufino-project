namespace PeopleManagement.API.Authorization
{
    /// <summary>Desfecho de uma checagem de permissao UMA contra o servidor de autorizacao.</summary>
    public enum ResourceAccessResult
    {
        /// <summary>O token vale e concede o recurso/escopos pedidos.</summary>
        Granted,

        /// <summary>O token vale, mas nao concede o recurso/escopos pedidos.</summary>
        Denied,

        /// <summary>O servidor de autorizacao recusou o proprio token (expirado, revogado, not-before).</summary>
        InvalidToken,

        /// <summary>O servidor de autorizacao nao foi alcancado ou respondeu com erro de servidor.</summary>
        ServerUnavailable,
    }

    public interface IAuthorizationServerClient
    {
        /// <summary>
        /// Busca o retrato com TODAS as permissoes que o token concede — uma pergunta por token,
        /// nao por endpoint.
        /// </summary>
        /// <remarks>
        /// E a chamada que sustenta o <see cref="IRptCache"/>. Vai ao endpoint de token com
        /// <c>response_mode=permissions</c> e <strong>sem</strong> o parametro <c>permission</c>:
        /// sem ele o Keycloak avalia tudo que a pessoa alcanca naquele resource server e devolve a
        /// lista.
        /// </remarks>
        Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default);
    }
}
