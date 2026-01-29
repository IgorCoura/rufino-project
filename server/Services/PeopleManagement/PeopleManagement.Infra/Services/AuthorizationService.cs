using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using System.Text.Json;

namespace PeopleManagement.Infra.Services
{
    public class AuthorizationService(HttpClient httpClient, ILogger<AuthorizationService> logger, AuthorizationOptions options, IMemoryCache cache) : IAuthorizationService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<AuthorizationService> _logger = logger;
        private readonly AuthorizationOptions _options = options;
        private readonly IMemoryCache _cache = cache;
        private const string TokenCacheKey = "KeycloakAccessToken";
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<string> GetAuthorizationToken()
        {
            // Tenta obter do cache primeiro
            if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                _logger.LogDebug("Using cached authorization token");
                return cachedToken;
            }

            // Thread-safe: apenas uma requisição por vez
            await _semaphore.WaitAsync();
            try
            {
                // Double-check: outro thread pode ter obtido o token
                if (_cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
                {
                    return cachedToken;
                }

                return await RequestNewToken();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> RequestNewToken()
        {
            _logger.LogInformation("Requesting new authorization token from Keycloak at {KeycloakUrl}", _options.KeycloakUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, _options.KeycloakUrl);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            });

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to obtain token from Keycloak. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to obtain authorization token: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonSerializer.Deserialize<JsonDocument>(content);

            if (document?.RootElement.TryGetProperty("access_token", out var tokenElement) != true)
            {
                throw new InvalidOperationException("Access token not found in Keycloak response");
            }

            var accessToken = tokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Access token is null or empty");
            }

            // Obtém o tempo de expiração e armazena em cache com margem de segurança
            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expiresElement)
                ? expiresElement.GetInt32()
                : 30; // Default 0.5 minutos

           
            var cacheExpiration = TimeSpan.FromSeconds(expiresIn * 0.5); // 50% do tempo de vida

            _cache.Set(TokenCacheKey, accessToken, cacheExpiration);

            _logger.LogInformation("Token cached for {Seconds} seconds", cacheExpiration.TotalSeconds);

            return accessToken;
        }


    }
}
