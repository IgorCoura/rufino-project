using PeopleManagement.Domain.AggregatesModel.PositionAggregate;

namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed class Role : Entity, IAggregateRoot
    {
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public CBO CBO { get; private set; } = null!;
        public Remuneration Remuneration { get; private set; } = null!;
        public Guid PositionId { get; private set; }
        public Guid CompanyId { get; private set; }

        private Role() { }
        private Role(Guid id, Name name, Description description, CBO cBO, Remuneration remuneration, Guid positionId, Guid companyId) : base(id)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            Remuneration = remuneration;
            PositionId = positionId;
            CompanyId = companyId;
        }

        public static Role Create(Guid id, Name name, Description description, CBO cBO, Remuneration remuneration, Guid positionId, Guid companyId) => new(id, name, description, cBO, remuneration, positionId, companyId);

        public void Edit(Name name, Description description, CBO cBO, Remuneration remuneration)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            Remuneration = remuneration;
        }
    }
}
