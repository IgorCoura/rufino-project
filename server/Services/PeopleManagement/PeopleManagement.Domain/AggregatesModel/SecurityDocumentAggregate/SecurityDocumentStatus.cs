namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class SecurityDocumentStatus : Enumeration
    {
        public static readonly SecurityDocumentStatus RequiredDocument = new(1, nameof(RequiredDocument));
        public static readonly SecurityDocumentStatus RequiredValidaty = new(2, nameof(RequiredValidaty));
        public static readonly SecurityDocumentStatus OK = new(3, nameof(OK));
        public static readonly SecurityDocumentStatus Deprecated = new(4, nameof(Deprecated));

        private SecurityDocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator SecurityDocumentStatus(int id) => Enumeration.FromValue<SecurityDocumentStatus>(id);
        public static implicit operator SecurityDocumentStatus(string name) => Enumeration.FromDisplayName<SecurityDocumentStatus>(name);
    }
}
