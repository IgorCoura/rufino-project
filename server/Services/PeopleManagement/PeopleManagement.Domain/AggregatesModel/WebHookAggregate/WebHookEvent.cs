namespace PeopleManagement.Domain.AggregatesModel.WebHookAggregate
{
    public class WebHookEvent : Enumeration
    {

        public static readonly WebHookEvent DocCreated = new(1, "doc_created");
        public static readonly WebHookEvent CreatedSigner = new(2, "created_signer");
        public static readonly WebHookEvent DocDeleted = new(3, "doc_deleted");
        public static readonly WebHookEvent DocSigned = new(4, "doc_signed");
        public static readonly WebHookEvent DocRefused = new(5, "doc_refused");
        public static readonly WebHookEvent EmailBounce = new(6, "email_bounce");
        public static readonly WebHookEvent SignatureNotificationSent = new(7, "signature_notification_sent");
        public static readonly WebHookEvent DocViewed = new(8, "doc_viewed");
        public static readonly WebHookEvent DocReadConfirmation = new(9, "doc_read_confirmation");
        public static readonly WebHookEvent SignerAuthenticationFailed = new(10, "signer_authentication_failed");
        public static readonly WebHookEvent DocExpired = new(11, "doc_expired");
        public static readonly WebHookEvent DocExpirationAlert = new(12, "doc_expiration_alert");



        private WebHookEvent(int id, string name) : base(id, name)
        {
        }

        public static implicit operator WebHookEvent(int id) => Enumeration.FromValue<WebHookEvent>(id);
        public static implicit operator WebHookEvent(string name) => Enumeration.FromDisplayName<WebHookEvent>(name);


    }
}
