namespace PeopleManagement.Domain.Options
{
    public class WhatsAppOptions
    {
        public const string SectionName = "WhatsApp";

        public string BaseUrl { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
