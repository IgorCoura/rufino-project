namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options
{
    public class AuthorizationOptions
    {
        public const string ConfigurationSection = "AuthorizationOptions";

        public string KeycloakUrl { get;  set; } = string.Empty;
        public string ClientId { get;  set; } = string.Empty;
        public string ClientSecret { get;  set; } = string.Empty;
    }
}
