namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options
{
    public class SignOptions
    {
        public const string ConfigurationSection = "SignOptions";
        public string AccessToken { get; set; } = string.Empty;
        public string WebHookUrl { get; set; } = "https://localhost:55020/api/v1/document/insert/signer";
        public string BaseUrl { get; set; } = "https://app.clicksign.com";
    }
}
