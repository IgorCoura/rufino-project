namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed class Company : Entity
    {      
        public string CorporateNmae { get; private set; } = string.Empty;
        public string FantasyName { get; private set; } = string.Empty;
        public string Cnpj { get; private set; } = string.Empty;
        public Address Address { get; private set; } = Address.Default();

        public Company(Guid id, string corporateNmae, string fantasyName, string cnpj, Address address) : base(id)
        {
            CorporateNmae = corporateNmae;
            FantasyName = fantasyName;
            Cnpj = cnpj;
            Address = address;
        }

        public static Company Create(string corporateNmae, string fantasyName, string cnpj, Address address)
        {
            var id = Guid.NewGuid();
            return new Company(id, corporateNmae, fantasyName, cnpj, address);
        }
    }
}
