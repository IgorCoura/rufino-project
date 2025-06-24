namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed class Company : Entity, IAggregateRoot
    {
        

        public NameCompany CorporateName { get; private set; } = null!;
        public NameFantasy FantasyName { get; private set; } = null!;
        public CNPJ Cnpj { get; private set; } = null!;
        public Contact Contact { get; private set; } = null!;
        public Address Address { get; private set; } = null!;

        private Company(Guid id, NameCompany corporateName, NameFantasy fantasyName, CNPJ cnpj, Contact contact, Address address) : base(id)
        {
            CorporateName = corporateName;
            FantasyName = fantasyName;
            Cnpj = cnpj;
            Contact = contact;
            Address = address;
        }

        private Company()
        {
        }

        public static Company Create(Guid id, NameCompany corporateName, NameFantasy fantasyName, CNPJ cnpj, Contact contact, Address address)
        {
            return new Company(id, corporateName, fantasyName, cnpj, contact, address);
        }

        public void Edit(NameCompany corporateName, NameFantasy fantasyName, CNPJ cnpj, Contact contact, Address address)
        {
            CorporateName = corporateName;
            FantasyName = fantasyName;
            Cnpj = cnpj;
            Contact = contact;
            Address = address;
        }
    }
}
