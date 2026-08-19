namespace BillPayment.IntegrationTests.Authorization;

using System.Security.Claims;
using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Prova que token inválido sai como 401, servidor de autorização fora do ar sai como 503, e
/// negativa simples de permissão continua 403.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a correção que o ADR-004 do TenantManagement diz que a cópia carrega</strong> — e
/// que ninguém verifica olhando o código, porque o defeito é o colapso dos três casos num 403 só.
/// Um cliente que recebe 403 por token expirado não renova o token: ele mostra "sem permissão"
/// para alguém que tem permissão. E 403 por Keycloak fora do ar esconde uma indisponibilidade
/// atrás de uma mensagem de autorização.
/// </para>
/// <para>
/// Sem containers e sem HTTP: exercita os dois handlers direto, porque o que se mede aqui é a
/// tradução de resultado em status, não o pipeline.
/// </para>
/// </remarks>
public sealed class AuthorizationStatusMappingTests
{
    private sealed class StubAuthorizationServerClient(ResourceAccessResult result) : IAuthorizationServerClient
    {
        public Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    /// <summary>Registra qual verbo de autenticação o tradutor chamou, sem precisar do pipeline.</summary>
    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public bool Challenged { get; private set; }

        public bool Forbidden { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            Challenged = true;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            Forbidden = true;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private static async Task<AuthorizationHandlerContext> RunRequirementHandlerAsync(ResourceAccessResult clientResult)
    {
        var requirement = new ProtectedResourceRequirement("bill#approve");
        var handler = new ProtectedResourceRequirementHandler(
            new HttpContextAccessor(),
            new StubAuthorizationServerClient(clientResult));

        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);
        await handler.HandleAsync(context);
        return context;
    }

    private static (DefaultHttpContext HttpContext, RecordingAuthenticationService AuthService) CreateHttpContext()
    {
        var authService = new RecordingAuthenticationService();
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(authService);
        services.AddLogging();
        services.AddOptions();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        httpContext.Response.Body = new MemoryStream();
        return (httpContext, authService);
    }

    private static AuthorizationPolicy AnyPolicy()
        => new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

    // Escopo concedido pelo servidor de autorização faz o requirement passar.
    [Fact]
    public async Task RequirementHandler_WhenAccessIsGranted_ShouldSucceed()
    {
        var context = await RunRequirementHandlerAsync(ResourceAccessResult.Granted);

        Assert.True(context.HasSucceeded);
    }

    // Token recusado pelo servidor de autorização falha com a razão que vira 401 lá na frente.
    [Fact]
    public async Task RequirementHandler_WhenTheTokenIsRejected_ShouldFailWithInvalidTokenReason()
    {
        var context = await RunRequirementHandlerAsync(ResourceAccessResult.InvalidToken);

        Assert.True(context.HasFailed);
        Assert.Contains(context.FailureReasons, r => r is InvalidTokenFailureReason);
    }

    // Servidor de autorização inalcançável falha com a razão que vira 503, não com a de token.
    [Fact]
    public async Task RequirementHandler_WhenTheServerIsUnreachable_ShouldFailWithUnavailableReason()
    {
        var context = await RunRequirementHandlerAsync(ResourceAccessResult.ServerUnavailable);

        Assert.True(context.HasFailed);
        Assert.Contains(context.FailureReasons, r => r is AuthorizationServerUnavailableFailureReason);
    }

    // Negativa simples de permissão falha SEM razão especial — é o 403 legítimo.
    [Fact]
    public async Task RequirementHandler_OnPlainDenial_ShouldFailWithoutSpecialReason()
    {
        var context = await RunRequirementHandlerAsync(ResourceAccessResult.Denied);

        Assert.True(context.HasFailed);
        Assert.DoesNotContain(context.FailureReasons, r => r is InvalidTokenFailureReason);
        Assert.DoesNotContain(context.FailureReasons, r => r is AuthorizationServerUnavailableFailureReason);
    }

    // Token inválido vira DESAFIO 401, para o cliente renovar — nunca 403, que ele não sabe tratar.
    [Fact]
    public async Task ResultHandler_WhenTheReasonIsAnInvalidToken_ShouldChallengeWith401()
    {
        var (httpContext, authService) = CreateHttpContext();
        var failure = AuthorizationFailure.Failed([new InvalidTokenFailureReason(null!)]);
        var result = PolicyAuthorizationResult.Forbid(failure);

        await new AuthorizationResultHandler().HandleAsync(
            _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

        Assert.True(authService.Challenged);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    // Keycloak fora do ar vira 503: indisponibilidade não pode ser reportada como falta de permissão.
    [Fact]
    public async Task ResultHandler_WhenTheAuthorizationServerIsUnreachable_ShouldAnswer503()
    {
        var (httpContext, authService) = CreateHttpContext();
        var failure = AuthorizationFailure.Failed([new AuthorizationServerUnavailableFailureReason(null!)]);
        var result = PolicyAuthorizationResult.Forbid(failure);

        await new AuthorizationResultHandler().HandleAsync(
            _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

        Assert.False(authService.Challenged);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
    }

    // Negativa de permissão continua 403 — o tradutor não pode transformar TODO 403 em outra coisa.
    [Fact]
    public async Task ResultHandler_OnPlainPermissionDenial_ShouldKeepThe403()
    {
        var (httpContext, authService) = CreateHttpContext();
        var result = PolicyAuthorizationResult.Forbid();

        await new AuthorizationResultHandler().HandleAsync(
            _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

        Assert.True(authService.Forbidden);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
    }

    // Autorização bem-sucedida segue o pipeline: o tradutor não pode engolir a requisição.
    [Fact]
    public async Task ResultHandler_WhenAuthorizationSucceeds_ShouldInvokeThePipeline()
    {
        var (httpContext, _) = CreateHttpContext();
        var nextInvoked = false;

        await new AuthorizationResultHandler().HandleAsync(
            _ => { nextInvoked = true; return Task.CompletedTask; },
            httpContext, AnyPolicy(), PolicyAuthorizationResult.Success());

        Assert.True(nextInvoked);
    }
}
