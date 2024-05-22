namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed class Department : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;

        private Department() { }
        private Department(Name name, Description description)
        {
            Name = name;
            Description = description;
        }

        public static Department Create(Name name, Description description) => new(name, description);
    }
}
