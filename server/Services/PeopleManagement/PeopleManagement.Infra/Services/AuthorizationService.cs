using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using System.Text.Json;

namespace PeopleManagement.Infra.Services
{
    public class AuthorizationService(HttpClient httpClient, ILogger<AuthorizationService> logger, AuthorizationOptions options) : IAuthorizationService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<AuthorizationService> _logger = logger;
        private readonly AuthorizationOptions _options = options;

        public async Task<string> GetAuthorizationToken()
        {
            _logger.LogInformation("Requesting authorization token from Keycloak at {KeycloakUrl}", _options.KeycloakUrl);
            var request = new HttpRequestMessage(HttpMethod.Post, _options.KeycloakUrl);
            request.Content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _options.ClientId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var accessToken = JsonSerializer.Deserialize<JsonElement>(content).GetProperty("access_token").GetString() ?? throw new NullReferenceException();
            return accessToken;
        }


    }
}
