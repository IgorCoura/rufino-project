namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentStatus : Enumeration
    {
        public static readonly DocumentStatus RequiresDocument = new(1, nameof(RequiresDocument));
        public static readonly DocumentStatus RequiresValidation = new(2, nameof(RequiresValidation));
        public static readonly DocumentStatus OK = new(3, nameof(OK));
        public static readonly DocumentStatus Deprecated = new(4, nameof(Deprecated));
        public static readonly DocumentStatus AwaitingSignature = new(5, nameof(AwaitingSignature));
        public static readonly DocumentStatus Warning = new(6, nameof(Warning));

        private DocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentStatus(int id) => Enumeration.FromValue<DocumentStatus>(id);
        public static implicit operator DocumentStatus(string name) => Enumeration.FromDisplayName<DocumentStatus>(name);
    }
}
