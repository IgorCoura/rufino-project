namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentStatus : Enumeration
    {
        public static readonly DocumentStatus RequiredDocument = new(1, nameof(RequiredDocument));
        public static readonly DocumentStatus RequiredValidaty = new(2, nameof(RequiredValidaty));
        public static readonly DocumentStatus OK = new(3, nameof(OK));
        public static readonly DocumentStatus Deprecated = new(4, nameof(Deprecated));
        public static readonly DocumentStatus AwaitingSignature = new(5, nameof(AwaitingSignature));

        private DocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentStatus(int id) => Enumeration.FromValue<DocumentStatus>(id);
        public static implicit operator DocumentStatus(string name) => Enumeration.FromDisplayName<DocumentStatus>(name);
    }
}
