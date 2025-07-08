namespace PeopleManagement.Domain.AggregatesModel.DepartmentAggregate
{
    public sealed class Department : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public Guid CompanyId { get; private set; }

        private Department() { }
        private Department(Guid id, Name name, Description description, Guid companyId) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
        }

        public static Department Create(Guid id, Name name, Description description, Guid companyId) => new(id, name, description, companyId);

        public void Edit(Name name, Description description)
        {
            Name = name;
            Description = description;
        }
    }
}
