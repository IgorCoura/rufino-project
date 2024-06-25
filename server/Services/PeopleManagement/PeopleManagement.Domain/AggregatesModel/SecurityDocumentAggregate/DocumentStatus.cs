namespace PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate
{
    public sealed class DocumentStatus : Enumeration
    {
        public static readonly DocumentStatus Pending = new(1, nameof(Pending));
        public static readonly DocumentStatus OK = new(2, nameof(OK));
        public static readonly DocumentStatus Deprecated = new(3, nameof(Deprecated));
        public static readonly DocumentStatus Invalid = new(4, nameof(Invalid));
        public static readonly DocumentStatus RequiredValidaty = new(5, nameof(RequiredValidaty));
        private DocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentStatus(int id) => Enumeration.FromValue<DocumentStatus>(id);
        public static implicit operator DocumentStatus(string name) => Enumeration.FromDisplayName<DocumentStatus>(name);
    }
}
