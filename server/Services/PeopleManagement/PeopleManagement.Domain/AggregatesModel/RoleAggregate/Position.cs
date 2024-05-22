namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public class Position : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public CBO CBO { get; private set; } = null!;
        public Department? Department { get; private set; }
        public Guid DepartmentId { get; private set; }

        public Position() { }

        public Position(Name name, Description description, CBO cBO, Guid departmentId)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            DepartmentId = departmentId;
        }
        
        public static Position Create(Name name, Description description, CBO cBO, Guid departmentId) => new(name, description, cBO, departmentId);

    }
}
