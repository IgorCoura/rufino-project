namespace TenantManagement.API.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Marks an authorization failure caused by the authorization server rejecting
/// the propagated access token (expired, revoked, not-before).
/// </summary>
public sealed class InvalidTokenFailureReason(IAuthorizationHandler handler)
    : AuthorizationFailureReason(handler, "The authorization server rejected the access token.");

/// <summary>
/// Marks an authorization failure caused by the authorization server being
/// unreachable or answering with a server error.
/// </summary>
public sealed class AuthorizationServerUnavailableFailureReason(IAuthorizationHandler handler)
    : AuthorizationFailureReason(handler, "The authorization server could not be reached.");

/// <summary>
/// Translates authorization failures into the proper HTTP status: an invalid
/// token becomes a 401 challenge (so clients can refresh/relogin) and an
/// unreachable authorization server becomes 503, instead of both collapsing
/// into the default 403 Forbidden.
/// </summary>
public class AuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && authorizeResult.AuthorizationFailure is { } failure)
        {
            if (failure.FailureReasons.Any(r => r is InvalidTokenFailureReason))
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }

                return;
            }

            if (failure.FailureReasons.Any(r => r is AuthorizationServerUnavailableFailureReason))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Authorization server unavailable\"}");
                return;
            }
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
