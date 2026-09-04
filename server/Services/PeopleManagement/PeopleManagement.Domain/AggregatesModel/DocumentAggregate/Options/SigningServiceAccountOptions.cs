namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options
{
    /// <summary>
    /// Credenciais da conta de serviço que a integração de assinatura usa para obter um token
    /// próprio no Keycloak — é com ele que o gerenciador de webhook fala com a nossa API.
    /// </summary>
    /// <remarks>
    /// Chamava-se <c>AuthorizationOptions</c> e ligava na seção de mesmo nome, o que produzia duas
    /// armadilhas de uma vez: colidia com a <c>API.Authorization.AuthorizationOptions</c> (que liga
    /// na seção <c>Keycloak</c> e é a autorização de verdade), e a seção que se chamava
    /// "AuthorizationOptions" guardava credencial de um fornecedor de assinatura. Renomeada em
    /// 2026-09-04 junto com a seção, que virou <c>DocumentSigning:ServiceAccount</c>.
    /// </remarks>
    public class SigningServiceAccountOptions
    {
        public const string ConfigurationSection = "DocumentSigning:ServiceAccount";

        /// <summary>Endpoint de token do realm. Era <c>KeycloakUrl</c>.</summary>
        public string TokenEndpoint { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        /// <summary>Nunca vem do <c>appsettings</c> versionado — env var ou user-secrets.</summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}
