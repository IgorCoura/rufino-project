using Microsoft.AspNetCore.Authorization;

namespace Commom.Auth.Authorization
{
    public class AuthorizationIdAttribute : AuthorizeAttribute
    {
        public const string POLICY_PREFIX = AuthorizationIdOptions.POLICY_PREFIX;

        public AuthorizationIdAttribute(string id) => Id = id;

        public string Id 
        {
            get
            {
                return Policy![POLICY_PREFIX.Length..];
            }
            set
            {
                Policy = $"{POLICY_PREFIX}{value}";
            }
        }
    }
}
