namespace PeopleManagement.API.Authorization
{
    /// <summary>
    /// Outcome of a UMA permission check against the authorization server.
    /// </summary>
    public enum ResourceAccessResult
    {
        /// <summary>The token is valid and grants the requested resource/scopes.</summary>
        Granted,

        /// <summary>The token is valid but does not grant the requested resource/scopes.</summary>
        Denied,

        /// <summary>The authorization server rejected the access token itself (expired, revoked, not-before).</summary>
        InvalidToken,

        /// <summary>The authorization server could not be reached or answered with a server error.</summary>
        ServerUnavailable,
    }

    public interface IAuthorizationServerClient
    {
        Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default);
    }
}
