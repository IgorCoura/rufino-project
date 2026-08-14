namespace TenantManagement.Infra.Identity;

using System.Text.Json.Serialization;

/// <summary>
/// Os pedaços da representação de usuário do provedor que este BC usa. Tudo que não está
/// aqui é ignorado na desserialização e <strong>reescrito com o que veio</strong> na
/// atualização — por isso a atualização parte da representação lida, nunca de um objeto novo.
/// </summary>
internal sealed record KeycloakUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; init; }

    [JsonPropertyName("requiredActions")]
    public List<string>? RequiredActions { get; init; }
}

internal sealed record KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
