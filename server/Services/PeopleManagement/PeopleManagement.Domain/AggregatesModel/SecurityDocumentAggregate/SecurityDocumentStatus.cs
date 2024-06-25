namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public class SecurityDocumentStatus : Enumeration
    {
        public static SecurityDocumentStatus RequiredDocument => new(1, nameof(RequiredDocument));
        public static SecurityDocumentStatus RequiredValidaty => new(2, nameof(RequiredValidaty));
        public static SecurityDocumentStatus OK => new(3, nameof(OK));
        public static SecurityDocumentStatus Deprecated => new(4, nameof(Deprecated));
        private SecurityDocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator SecurityDocumentStatus(int id) => Enumeration.FromValue<SecurityDocumentStatus>(id);
        public static implicit operator SecurityDocumentStatus(string name) => Enumeration.FromDisplayName<SecurityDocumentStatus>(name);
    }
}
