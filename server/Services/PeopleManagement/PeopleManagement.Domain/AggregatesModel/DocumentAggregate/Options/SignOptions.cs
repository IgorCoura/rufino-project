namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options
{
    public class SignOptions
    {
        public const string ConfigurationSection = "SignOptions";

        public string WeebHookUrl { get; private set; } = "https://localhost:55020/api/v1/document/insert/signer";
    }
}
