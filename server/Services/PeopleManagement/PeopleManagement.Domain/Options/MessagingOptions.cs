namespace PeopleManagement.Domain.Options
{
    public class MessagingOptions
    {
        public const string SectionName = "Messaging";

        public string BaseUrl { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string HealthCheckNumber { get; set; } = string.Empty;
    }
}
