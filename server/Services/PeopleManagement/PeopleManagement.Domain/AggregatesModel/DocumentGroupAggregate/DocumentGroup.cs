namespace PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate
{
    public sealed class DocumentGroup : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public Guid CompanyId { get; private set; }

        private DocumentGroup() { }
        private DocumentGroup(Guid id, Name name, Description description, Guid companyId) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
        }

        public static DocumentGroup Create(Guid id, Name name, Description description, Guid companyId) => new(id, name, description, companyId);

        public void Edit(Name name, Description description)
        {
            Name = name;
            Description = description;
        }
    }
}
