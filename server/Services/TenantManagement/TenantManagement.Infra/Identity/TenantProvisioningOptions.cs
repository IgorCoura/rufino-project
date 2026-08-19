namespace TenantManagement.Infra.Identity;

/// <summary>
/// Configuração do adapter que leva o acesso ao provedor de identidade.
/// </summary>
/// <remarks>
/// O segredo <strong>nunca</strong> fica no <c>appsettings.json</c>: variável de ambiente em
/// produção (<c>TenantProvisioning__ClientSecret</c>) ou <c>dotnet user-secrets</c> em
/// desenvolvimento. É a mesma regra do ADR-009 do BillPayment.
/// <para>
/// Este cliente é <strong>separado</strong> do que a API usa para autorizar requisições: um só
/// responde ticket de permissão, o outro cria pessoa e concede tenant. Raio de estrago
/// diferente pede segredo diferente.
/// </para>
/// </remarks>
public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";

    /// <summary>Desligado por padrão: ligar a escrita no provedor é decisão de quem configura.</summary>
    public bool Enabled { get; set; }

    public string AuthServerUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Atributo multivalorado da pessoa que lista os tenants dela. É o que o mapper do realm
    /// expõe como claim <c>tenants</c> — a identidade, sem recorte de produto.
    /// </summary>
    public string TenantsAttribute { get; set; } = "tenants";

    /// <summary>
    /// Atributo por produto: além do <c>tenants</c> genérico, cada produto tem o seu, com os
    /// tenants em que a pessoa tem vínculo ativo <strong>e</strong> aquele produto está habilitado.
    /// É o que faz o produto governar o acesso — sem isto, quem tem o tenant no token usa todos
    /// os produtos, inclusive os que o tenant nunca contratou.
    /// </summary>
    /// <remarks>
    /// <strong>O nome importa e a escolha não é livre.</strong> O guard dos produtos casa o TIPO do
    /// claim por <c>Contains</c>: <c>"bp_tenants".Contains("tenants")</c> é verdadeiro, então um
    /// produto configurado para ler <c>tenants</c> aceitaria também os valores de <c>bp_tenants</c>.
    /// O sentido que importa está seguro — <c>"tenants".Contains("bp_tenants")</c> é falso, logo o
    /// BillPayment NÃO aceita o claim genérico. Ao acrescentar produto, escolha um nome que
    /// nenhum outro contenha.
    /// </remarks>
    public Dictionary<string, string> ProductAttributes { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BillPayment"] = "bp_tenants",
        ["PeopleManagement"] = "pm_tenants",
    };

    /// <summary>Convite por e-mail para quem foi criado agora (definir senha e verificar e-mail).</summary>
    public bool SendInvitationEmail { get; set; } = true;

    /// <summary>Cliente para o qual o link do convite aponta.</summary>
    public string InvitationClientId { get; set; } = string.Empty;

    public string? InvitationRedirectUri { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured
        => Enabled
           && !string.IsNullOrWhiteSpace(AuthServerUrl)
           && !string.IsNullOrWhiteSpace(Realm)
           && !string.IsNullOrWhiteSpace(ClientId)
           && !string.IsNullOrWhiteSpace(ClientSecret);

    public string RealmBaseUrl => $"{Normalize(AuthServerUrl)}realms/{Realm}/";

    public string AdminBaseUrl => $"{Normalize(AuthServerUrl)}admin/realms/{Realm}/";

    private static string Normalize(string url)
        => string.IsNullOrWhiteSpace(url) || url.EndsWith('/') ? url : url + "/";
}
