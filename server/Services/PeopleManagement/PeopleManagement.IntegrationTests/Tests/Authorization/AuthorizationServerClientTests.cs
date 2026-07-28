using System.Net;
using System.Text;
using PeopleManagement.API.Authorization;
using AuthorizationOptions = PeopleManagement.API.Authorization.AuthorizationOptions;

namespace PeopleManagement.IntegrationTests.Tests.Authorization
{
    /// <summary>
    /// Unit tests (no containers) for the mapping between Keycloak UMA responses
    /// and <see cref="ResourceAccessResult"/>.
    /// </summary>
    public class AuthorizationServerClientTests
    {
        private static AuthorizationServerClient CreateClient(HttpMessageHandler handler)
        {
            var options = new AuthorizationOptions
            {
                AuthServerUrl = "http://keycloak.test",
                Realm = "rufino",
                Resource = "people-management-api",
            };

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(options.KeycloakUrlRealm),
            };

            return new AuthorizationServerClient(httpClient, options);
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(responder(request));
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException("connection refused");
        }

        private static HttpResponseMessage Response(HttpStatusCode statusCode, string body = "{}")
            => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task Returns_InvalidToken_when_keycloak_rejects_the_access_token_with_401()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Unauthorized,
                "{\"error\":\"invalid_grant\",\"error_description\":\"Invalid bearer token\"}")));

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.InvalidToken, result);
        }

        [Fact]
        public async Task Returns_Denied_when_keycloak_answers_403_access_denied()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.Forbidden,
                "{\"error\":\"access_denied\"}")));

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.Denied, result);
        }

        [Fact]
        public async Task Returns_Denied_when_keycloak_answers_400_bad_request()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_request\"}")));

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.Denied, result);
        }

        [Fact]
        public async Task Returns_ServerUnavailable_when_keycloak_answers_a_server_error()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.BadGateway, "")));

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.ServerUnavailable, result);
        }

        [Fact]
        public async Task Returns_ServerUnavailable_when_the_request_fails_at_the_network_level()
        {
            var client = CreateClient(new ThrowingHandler());

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.ServerUnavailable, result);
        }

        [Fact]
        public async Task Returns_Granted_on_success_for_a_single_scope_permission()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
                "{\"result\":true}")));

            var result = await client.VerifyAccessToResouce("employee#view");

            Assert.Equal(ResourceAccessResult.Granted, result);
        }

        [Fact]
        public async Task Returns_Granted_when_all_requested_scopes_are_present_in_the_rpt()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
                "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"upload\",\"send2sign\"]}]")));

            var result = await client.VerifyAccessToResouce("document#upload,send2sign");

            Assert.Equal(ResourceAccessResult.Granted, result);
        }

        [Fact]
        public async Task Returns_Denied_when_a_requested_scope_is_missing_from_the_rpt()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
                "[{\"rsid\":\"1\",\"rsname\":\"document\",\"scopes\":[\"upload\"]}]")));

            var result = await client.VerifyAccessToResouce("document#upload,send2sign");

            Assert.Equal(ResourceAccessResult.Denied, result);
        }

        [Fact]
        public async Task Returns_Denied_instead_of_throwing_when_the_resource_is_absent_from_the_rpt()
        {
            var client = CreateClient(new StubHandler(_ => Response(HttpStatusCode.OK,
                "[{\"rsid\":\"1\",\"rsname\":\"other-resource\",\"scopes\":[\"view\"]}]")));

            var result = await client.VerifyAccessToResouce("document#upload,send2sign");

            Assert.Equal(ResourceAccessResult.Denied, result);
        }
    }
}
