namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public sealed class DocumentUnitStatus : Enumeration
    {
        const int PENDING = 1;
        const int OKEY = 2;
        const int DEPRECATED = 3;
        const int INVALID = 4;
        const int REQUIRES_VALIDATION = 5;
        const int NOT_APPLICABLE = 6;
        const int AWAITING_SIGNATURE = 7;
        const int WARNING = 8;

        public static readonly DocumentUnitStatus Pending = new(PENDING, nameof(Pending));
        public static readonly DocumentUnitStatus OK = new(OKEY, nameof(OK));
        public static readonly DocumentUnitStatus Deprecated = new(DEPRECATED, nameof(Deprecated));
        public static readonly DocumentUnitStatus Invalid = new(INVALID, nameof(Invalid));
        public static readonly DocumentUnitStatus RequiresValidation = new(REQUIRES_VALIDATION, nameof(RequiresValidation));
        public static readonly DocumentUnitStatus NotApplicable = new(NOT_APPLICABLE, nameof(NotApplicable));
        public static readonly DocumentUnitStatus AwaitingSignature = new(AWAITING_SIGNATURE, nameof(AwaitingSignature));
        public static readonly DocumentUnitStatus Warning = new(WARNING, nameof(Warning));
        private DocumentUnitStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentUnitStatus(int id) => Enumeration.FromValue<DocumentUnitStatus>(id);
        public static implicit operator DocumentUnitStatus(string name) => Enumeration.FromDisplayName<DocumentUnitStatus>(name);

        public static int GetOrder(DocumentUnitStatus status)
        {
            return status.Id switch
            {
                OKEY => 0,
                NOT_APPLICABLE => 1,
                AWAITING_SIGNATURE => 2,
                REQUIRES_VALIDATION => 3,
                PENDING => 4,
                WARNING => 5,
                DEPRECATED => 6,
                INVALID => 7,
                _ => 7 // Invalid
            };
        }
    }
}

