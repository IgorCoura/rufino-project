namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentStatus : Enumeration
    {
        const int REQUIRES_DOCUMENT = 1;
        const int REQUIRES_VALIDATION = 2;
        const int OKAY = 3;
        const int DEPRECATED = 4;
        const int AWAITING_SIGNATURE = 5;
        const int WARNING = 6;

        public static readonly DocumentStatus RequiresDocument = new(REQUIRES_DOCUMENT, nameof(RequiresDocument));
        public static readonly DocumentStatus RequiresValidation = new(REQUIRES_VALIDATION, nameof(RequiresValidation));
        public static readonly DocumentStatus OK = new(OKAY, nameof(OK));
        public static readonly DocumentStatus Deprecated = new(DEPRECATED, nameof(Deprecated));
        public static readonly DocumentStatus AwaitingSignature = new(AWAITING_SIGNATURE, nameof(AwaitingSignature));
        public static readonly DocumentStatus Warning = new(WARNING, nameof(Warning));

        private DocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentStatus(int id) => Enumeration.FromValue<DocumentStatus>(id);
        public static implicit operator DocumentStatus(string name) => Enumeration.FromDisplayName<DocumentStatus>(name);

        public static int GetOrder(DocumentStatus status)
        {
            return status.Id switch
            {
                DEPRECATED => 0,
                WARNING => 3,
                REQUIRES_VALIDATION => 0,
                AWAITING_SIGNATURE => 1,
                REQUIRES_DOCUMENT => 2,
                OKAY => 4,
                _ => 6 // Invalid
            };
        }
    }
}
