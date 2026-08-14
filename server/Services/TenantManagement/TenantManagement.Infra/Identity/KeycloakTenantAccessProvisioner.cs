namespace TenantManagement.Infra.Identity;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TenantManagement.Domain.Ports;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Leva a concessão de acesso ao Keycloak pela Admin API.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Todas as operações são idempotentes.</strong> Conceder duas vezes deixa o mesmo
/// valor no atributo; revogar o que não existe não falha. É o que permite ao
/// reprovisionamento reexecutar sem medo.
/// </para>
/// <para>
/// <strong>A atualização parte da representação lida.</strong> O <c>PUT</c> de usuário do
/// Keycloak substitui o mapa de atributos inteiro: montar um objeto novo só com
/// <c>tenants</c> apagaria todo o resto — inclusive o <c>companies</c> de que o
/// PeopleManagement depende.
/// </para>
/// <para>
/// Nenhum termo de Keycloak cruza a porta <see cref="ITenantAccessProvisioner"/>. Trocar de
/// provedor é escrever outro adapter e mudar uma linha de configuração.
/// </para>
/// </remarks>
internal sealed class KeycloakTenantAccessProvisioner(
    HttpClient httpClient,
    IOptions<TenantProvisioningOptions> options,
    TimeProvider timeProvider,
    ILogger<KeycloakTenantAccessProvisioner> logger) : ITenantAccessProvisioner, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] InvitationActions = ["UPDATE_PASSWORD", "VERIFY_EMAIL"];

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly TenantProvisioningOptions _options = options.Value;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public async Task<AccessGrantResult> GrantAccessAsync(
        TenantId tenantId,
        string emailAddress,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var email = EmailSyntax.Normalize(emailAddress);
        var tenant = tenantId.ToString();

        var user = await FindUserByEmailAsync(email, cancellationToken);
        var created = false;

        if (user is null)
        {
            await CreateUserAsync(email, displayName, tenant, cancellationToken);
            user = await FindUserByEmailAsync(email, cancellationToken)
                ?? throw new InvalidOperationException($"Usuário {email} não foi encontrado logo após ser criado.");
            created = true;

            if (_options.SendInvitationEmail)
                await SendInvitationAsync(user.Id, cancellationToken);
        }
        else
        {
            await WriteTenantsAsync(user, Union(CurrentTenants(user), tenant), cancellationToken);
        }

        if (!Guid.TryParse(user.Id, out var userId))
            throw new InvalidOperationException($"O provedor devolveu um identificador que não é GUID para {email}.");

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Acesso do tenant {TenantId} concedido no provedor de identidade (usuário criado: {Created}).",
                tenantId,
                created);

        return new AccessGrantResult(UserId.From(userId), created);
    }

    public async Task RevokeAccessAsync(
        TenantId tenantId,
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        var email = EmailSyntax.Normalize(emailAddress);
        var tenant = tenantId.ToString();

        var user = await FindUserByEmailAsync(email, cancellationToken);
        if (user is null)
            return;

        var remaining = CurrentTenants(user)
            .Where(t => !string.Equals(t, tenant, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // A pessoa não é apagada mesmo sem nenhum tenant: ela pode voltar, e apagá-la
        // destruiria histórico de login que não é deste BC decidir sobre.
        await WriteTenantsAsync(user, remaining, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Acesso do tenant {TenantId} revogado no provedor de identidade.", tenantId);
    }

    private List<string> CurrentTenants(KeycloakUser user)
        => user.Attributes is not null && user.Attributes.TryGetValue(_options.TenantsAttribute, out var values)
            ? values
            : [];

    private static List<string> Union(List<string> current, string tenant)
    {
        var result = current.ToList();
        if (!result.Exists(t => string.Equals(t, tenant, StringComparison.OrdinalIgnoreCase)))
            result.Add(tenant);
        return result;
    }

    private async Task<KeycloakUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.AdminBaseUrl}users?email={Uri.EscapeDataString(email)}&exact=true");

        using var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUser>>(Json, cancellationToken);
        return users?.Count > 0 ? users[0] : null;
    }

    private async Task CreateUserAsync(string email, string displayName, string tenant, CancellationToken cancellationToken)
    {
        var payload = new KeycloakUser
        {
            Username = email,
            Email = email,
            FirstName = displayName,
            Enabled = true,
            EmailVerified = false,
            Attributes = new Dictionary<string, List<string>>(StringComparer.Ordinal)
            {
                [_options.TenantsAttribute] = [tenant],
            },
            RequiredActions = [.. InvitationActions],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.AdminBaseUrl}users")
        {
            Content = JsonContent.Create(payload, options: Json),
        };

        using var response = await SendAsync(request, cancellationToken);

        // 409 é outra requisição tendo criado a mesma pessoa no meio do caminho — o chamador
        // relê logo em seguida e segue com o que existe.
        if (response.StatusCode != HttpStatusCode.Conflict)
            response.EnsureSuccessStatusCode();
    }

    private async Task WriteTenantsAsync(KeycloakUser user, List<string> tenants, CancellationToken cancellationToken)
    {
        var attributes = user.Attributes is null
            ? new Dictionary<string, List<string>>(StringComparer.Ordinal)
            : new Dictionary<string, List<string>>(user.Attributes, StringComparer.Ordinal);

        if (tenants.Count == 0)
            attributes.Remove(_options.TenantsAttribute);
        else
            attributes[_options.TenantsAttribute] = tenants;

        var payload = user with { Attributes = attributes };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.AdminBaseUrl}users/{user.Id}")
        {
            Content = JsonContent.Create(payload, options: Json),
        };

        using var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendInvitationAsync(string userId, CancellationToken cancellationToken)
    {
        var redirect = string.IsNullOrWhiteSpace(_options.InvitationRedirectUri)
            ? string.Empty
            : $"&redirect_uri={Uri.EscapeDataString(_options.InvitationRedirectUri)}";

        var query = string.IsNullOrWhiteSpace(_options.InvitationClientId)
            ? string.Empty
            : $"?client_id={Uri.EscapeDataString(_options.InvitationClientId)}{redirect}";

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_options.AdminBaseUrl}users/{userId}/execute-actions-email{query}")
        {
            Content = JsonContent.Create(InvitationActions, options: Json),
        };

        using var response = await SendAsync(request, cancellationToken);

        // O convite é o único passo que pode falhar sem invalidar a concessão: o acesso já
        // existe, e o que falta é a pessoa saber. Derrubar aqui marcaria como falho um
        // provisionamento que funcionou.
        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Não foi possível enviar o convite por e-mail: {Status}.", response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await GetAccessTokenAsync(cancellationToken));

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && timeProvider.GetUtcNow() < _accessTokenExpiresAt)
            return _accessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && timeProvider.GetUtcNow() < _accessTokenExpiresAt)
                return _accessToken;

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
            });

            using var response = await httpClient.PostAsync(
                new Uri($"{_options.RealmBaseUrl}protocol/openid-connect/token"),
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(Json, cancellationToken)
                ?? throw new InvalidOperationException("O provedor de identidade devolveu um token vazio.");

            _accessToken = token.AccessToken;

            // Trinta segundos de folga: um token que expira no voo faz a operação seguinte
            // falhar com 401 e o vínculo ser marcado como falho sem motivo real.
            _accessTokenExpiresAt = timeProvider.GetUtcNow()
                .AddSeconds(Math.Max(token.ExpiresIn - 30, 5));

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"Keycloak({_options.Realm})");

    public void Dispose() => _tokenLock.Dispose();
}
