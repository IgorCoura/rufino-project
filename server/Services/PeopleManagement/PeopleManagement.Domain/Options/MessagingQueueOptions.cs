namespace PeopleManagement.Domain.Options
{
    public class MessagingQueueOptions
    {
        public const string SectionName = "Messaging:Queue";

        public string QueueName { get; set; } = "whatsapp";
        public int DelaySeconds { get; set; } = 5;
    }
}
