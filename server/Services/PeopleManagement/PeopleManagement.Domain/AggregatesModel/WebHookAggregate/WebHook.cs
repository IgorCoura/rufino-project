namespace PeopleManagement.Domain.AggregatesModel.WebHookAggregate
{
    public class WebHook : Entity
    {
        public string WebHookId { get; set; } = string.Empty;
        public WebHookEvent Event { get; private set; } = null!;
        private WebHook()
        {

        }

        public WebHook(Guid id, string webHookId, WebHookEvent webHookEvent) : base(id)
        {
            WebHookId = webHookId;
            Event = webHookEvent;
        }
    }

}
