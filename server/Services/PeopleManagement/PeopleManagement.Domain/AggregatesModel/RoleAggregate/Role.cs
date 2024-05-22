namespace PeopleManagement.Domain.AggregatesModel.RoleAggregate
{
    public sealed class Role : Entity, IAggregateRoot
    {       
        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public CBO CBO { get; private set; } = null!;
        public Remuneration Remuneration { get; private set; } = null!;
        public Position? Position { get; private set; }
        public Guid PositionId { get; private set; }

        private Role() { }
        private Role(Name name, Description description, CBO cBO, Remuneration remuneration, Guid positionId)
        {
            Name = name;
            Description = description;
            CBO = cBO;
            Remuneration = remuneration;
            PositionId = positionId;
        }

        public static Role Create(Name name, Description description, CBO cBO, Remuneration remuneration, Guid positionId) => new(name, description, cBO, remuneration, positionId);

    }
}
