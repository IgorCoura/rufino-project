namespace PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate
{
    public sealed class Workplace : Entity, IAggregateRoot
    {
        public Name Name { get; private set; } = null!;
        public Address Address { get; private set; } = null!;
        public Guid CompanyId { get; private set; }

        private Workplace() { }
        private Workplace(Guid id, Name name, Address address, Guid companyId) : base(id)
        {
            CompanyId = companyId;
            Name = name;
            Address = address;
        }

        public static Workplace Create(Guid id, Name name, Address address, Guid companyId) => new(id, name, address, companyId);

        public void Edit(Name name, Address address)
        {
            Name = name;
            Address = address;
        }
    }
}
 