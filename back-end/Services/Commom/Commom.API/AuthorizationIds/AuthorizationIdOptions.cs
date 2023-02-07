namespace Commom.API.AuthorizationIds
{
    public class AuthorizationIdOptions
    {
        public const string POLICY_PREFIX = "AuthorizationId";

        public const string Section = "Authorization";
        public string Schema { get; set; } = "Bearer";
    }
}
