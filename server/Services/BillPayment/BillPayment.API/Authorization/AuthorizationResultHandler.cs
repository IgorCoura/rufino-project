namespace BillPayment.API.Authorization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// Marca a falha causada pelo servidor de autorização ter recusado o token propagado
/// (expirado, revogado, not-before).
/// </summary>
public sealed class InvalidTokenFailureReason(IAuthorizationHandler handler)
    : AuthorizationFailureReason(handler, "The authorization server rejected the access token.");

/// <summary>
/// Marca a falha causada pelo servidor de autorização estar inalcançável ou ter respondido
/// com erro de servidor.
/// </summary>
public sealed class AuthorizationServerUnavailableFailureReason(IAuthorizationHandler handler)
    : AuthorizationFailureReason(handler, "The authorization server could not be reached.");

/// <summary>
/// Traduz falha de autorização para o status HTTP certo: token inválido vira 401 (para o cliente
/// renovar ou relogar) e servidor de autorização fora do ar vira 503 — em vez de os dois
/// colapsarem no 403 padrão, que faria o cliente tratar indisponibilidade como falta de permissão.
/// </summary>
public class AuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(authorizeResult);

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
