using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PeopleManagement.API.Authorization
{
    public class AuthorizationOptions
    {

        public const string Section = "Keycloak";

        private string authServerUrl = null!;
        public string Realm { get; set; } = default!;
        public string AuthServerUrl
        {
            get => this.authServerUrl;
            set => this.authServerUrl = NormalizeUrl(value);
        }
        public string KeycloakUrlRealm
        {
            get
            {
                return $"{this.AuthServerUrl}realms/{this.Realm}/";
            }
        }
        public string TokenEndpointPath { get; set; } = "protocol/openid-connect/token";
        public string SourceAuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
        public string SourceTokenName { get; set; } = "Bearer";
        public string GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:uma-ticket";
        
        public string Resource { get; set; } = string.Empty;

        public static bool DisableHeaderPropagation { get; set; }

        public bool UseProtectedResourcePolicyProvider { get; set; }

        public ScopesValidationMode ScopesValidationMode { get; set; } = ScopesValidationMode.AllOf;

        /// <summary>
        /// Liga o cache do retrato de permissoes. Desligado, cada endpoint volta a perguntar ao
        /// Keycloak — e o que a suite de integracao usa para exercitar o caminho de rede.
        /// </summary>
        public bool RptCacheEnabled { get; set; } = true;

        /// <summary>
        /// Por quanto tempo o retrato vale. E a janela em que uma permissao revogada no console
        /// ainda e aceita — mantenha curto. O teto real e sempre o <c>exp</c> do token.
        /// </summary>
        public TimeSpan RptCacheTtl { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Por quanto tempo, DEPOIS de vencido, o retrato ainda pode ser servido caso o servidor de
        /// autorizacao esteja fora do ar (<em>fail-static</em>). Zero desliga a degradacao e faz a
        /// indisponibilidade voltar a ser 503 imediato.
        /// </summary>
        public TimeSpan RptStaleGrace { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Teto de retratos guardados. Cada entrada e uma sessao ativa; estourado, o cache descarta
        /// as mais antigas e elas voltam a ser buscadas.
        /// </summary>
        public long RptCacheSizeLimit { get; set; } = 5_000;

        /// <summary>
        /// Nome do claim que lista os clientes desta pessoa. É <c>pm_tenants</c> — o claim POR
        /// PRODUTO emitido pelo BC TenantManagement (ADR-005 de lá), que traz só os tenants em que
        /// ela tem vínculo ativo <strong>e</strong> o PeopleManagement está habilitado. Substituiu
        /// o <c>companies</c> legado, que ninguém escrevia por código (era digitado à mão no
        /// console do Keycloak) e que suspender um cliente não afetava.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Não troque por <c>tenants</c>.</strong> O handler casa o TIPO do claim por
        /// <c>Contains</c>, e <c>"bp_tenants".Contains("tenants")</c> é verdadeiro — configurado
        /// com o genérico, este BC aceitaria também os tenants de quem só assinou o BillPayment.
        /// </para>
        /// <para>
        /// O valor é o mesmo Guid do <c>Company.Id</c>: é isso que o backfill do TenantManagement
        /// preserva, e por isso o <c>{company}</c> da rota não mudou de nome nem de conteúdo.
        /// </para>
        /// </remarks>
        public string RouteClaimTypeRequirement { get; set; } = "pm_tenants";
        public string RouteNameRequirement { get; set; } = "company";
        public string ResponseMode(bool isDecisionMode) => isDecisionMode ? "permissions" : "decision";


        private static string NormalizeUrl(string url)
        {
            if (!url.EndsWith('/'))
            {
                url += "/";
            }

            return url;
        }
    }

    /// <summary>
    /// Specifies the validation mode for multiple scopes.
    /// </summary>
    public enum ScopesValidationMode
    {
        /// <summary>
        /// Specifies that all of the scopes must be valid.
        /// </summary>
        AllOf,

        /// <summary>
        /// Specifies that at least one of the scopes must be valid.
        /// </summary>
        AnyOf,
    }
}
