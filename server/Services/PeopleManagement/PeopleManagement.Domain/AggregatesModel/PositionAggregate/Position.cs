namespace PeopleManagement.Domain.AggregatesModel.PositionAggregate
{
    public class Position : Entity
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public CBO CBO { get; private set; } = null!;
        public Guid DepartmentId { get; private set; }
        public Guid CompanyId { get; private set; }

        public Position() { }

        public Position(Guid id, Name name, Description description, CBO cBO, Guid departmentId, Guid companyId) : base(id)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            DepartmentId = departmentId;
            CompanyId = companyId;
        }

        public static Position Create(Guid id, Name name, Description description, CBO cBO, Guid departmentId, Guid companyId) => new(id, name, description, cBO, departmentId, companyId);

        public void Edit(Name name, Description description, CBO cBO)
        {
            Name = name;
            Description = description;
            CBO = cBO;
        }
    }
}
