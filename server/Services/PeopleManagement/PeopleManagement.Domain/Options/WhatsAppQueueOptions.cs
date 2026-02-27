namespace PeopleManagement.Domain.Options
{
    public class WhatsAppQueueOptions
    {
        public const string SectionName = "WhatsAppQueue";

        public string QueueName { get; set; } = "whatsapp";
        public int DelaySeconds { get; set; } = 5;
    }
}
