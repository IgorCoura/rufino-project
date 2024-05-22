namespace PeopleManagement.Domain.AggregatesModel.DepartmentAggregate
{
    public sealed class Department : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;

        private Department() { }
        private Department(Guid id, Name name, Description description) : base(id)
        {
            Name = name;
            Description = description;
        }

        public static Department Create(Guid id, Name name, Description description) => new(id, name, description);
    }
}
