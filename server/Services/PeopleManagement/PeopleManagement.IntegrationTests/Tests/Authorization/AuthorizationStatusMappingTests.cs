using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using PeopleManagement.API.Authorization;

namespace PeopleManagement.IntegrationTests.Tests.Authorization
{
    /// <summary>
    /// Unit tests (no containers) proving that an invalid token surfaces as 401,
    /// an unreachable authorization server as 503, and a plain permission denial
    /// as the default 403.
    /// </summary>
    public class AuthorizationStatusMappingTests
    {
        private sealed class StubAuthorizationServerClient(ResourceAccessResult result) : IAuthorizationServerClient
        {
            public Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default)
                => Task.FromResult(result);
        }

        /// <summary>
        /// Records which authentication verb the result handler invoked, without
        /// requiring a fully configured authentication pipeline.
        /// </summary>
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
            var requirement = new ProtectedResourceRequirement("employee#view");
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

        [Fact]
        public async Task Requirement_handler_succeeds_when_access_is_granted()
        {
            var context = await RunRequirementHandlerAsync(ResourceAccessResult.Granted);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task Requirement_handler_fails_with_invalid_token_reason_when_keycloak_rejects_the_token()
        {
            var context = await RunRequirementHandlerAsync(ResourceAccessResult.InvalidToken);

            Assert.True(context.HasFailed);
            Assert.Contains(context.FailureReasons, r => r is InvalidTokenFailureReason);
        }

        [Fact]
        public async Task Requirement_handler_fails_with_unavailable_reason_when_keycloak_is_unreachable()
        {
            var context = await RunRequirementHandlerAsync(ResourceAccessResult.ServerUnavailable);

            Assert.True(context.HasFailed);
            Assert.Contains(context.FailureReasons, r => r is AuthorizationServerUnavailableFailureReason);
        }

        [Fact]
        public async Task Requirement_handler_fails_without_special_reason_on_plain_permission_denial()
        {
            var context = await RunRequirementHandlerAsync(ResourceAccessResult.Denied);

            Assert.True(context.HasFailed);
            Assert.DoesNotContain(context.FailureReasons, r => r is InvalidTokenFailureReason);
            Assert.DoesNotContain(context.FailureReasons, r => r is AuthorizationServerUnavailableFailureReason);
        }

        [Fact]
        public async Task Result_handler_challenges_with_401_when_the_failure_reason_is_an_invalid_token()
        {
            var (httpContext, authService) = CreateHttpContext();
            var failure = AuthorizationFailure.Failed([new InvalidTokenFailureReason(null!)]);
            var result = PolicyAuthorizationResult.Forbid(failure);

            await new AuthorizationResultHandler().HandleAsync(
                _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

            Assert.True(authService.Challenged);
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Result_handler_answers_503_when_the_authorization_server_is_unreachable()
        {
            var (httpContext, authService) = CreateHttpContext();
            var failure = AuthorizationFailure.Failed([new AuthorizationServerUnavailableFailureReason(null!)]);
            var result = PolicyAuthorizationResult.Forbid(failure);

            await new AuthorizationResultHandler().HandleAsync(
                _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

            Assert.False(authService.Challenged);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Result_handler_keeps_the_default_403_on_plain_permission_denial()
        {
            var (httpContext, authService) = CreateHttpContext();
            var result = PolicyAuthorizationResult.Forbid();

            await new AuthorizationResultHandler().HandleAsync(
                _ => Task.CompletedTask, httpContext, AnyPolicy(), result);

            Assert.True(authService.Forbidden);
            Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Result_handler_invokes_the_pipeline_when_authorization_succeeds()
        {
            var (httpContext, _) = CreateHttpContext();
            var nextInvoked = false;

            await new AuthorizationResultHandler().HandleAsync(
                _ => { nextInvoked = true; return Task.CompletedTask; },
                httpContext, AnyPolicy(), PolicyAuthorizationResult.Success());

            Assert.True(nextInvoked);
        }
    }
}
