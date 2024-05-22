namespace PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate
{
    public sealed class Workplace : Entity, IAggregateRoot
    {
        public Name Name { get; private set; } = null!;
        public Address Address { get; private set; } = null!;

        private Workplace() { }
        private Workplace(Name name, Address address)
        {
            Name = name;
            Address = address;
        }

        public static Workplace Create(Name name, Address address) => new(name, address);
    }
}
 