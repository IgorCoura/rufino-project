namespace PeopleManagement.Domain.AggregatesModel.CompanyAggregate
{
    public sealed class Company : Entity
    {
        public Company(Guid id, string corporateName, string fantasyName, string description, string cnpj, string email, string phone, Address address) : base(id)
        {
            CorporateName = corporateName;
            FantasyName = fantasyName;
            Description = description;
            Cnpj = cnpj;
            Email = email;
            Phone = phone;
            Address = address;
        }

        private Company(Guid id) : base(id) { }

        public string CorporateName { get; private set; } = string.Empty;
        public string FantasyName { get; private set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cnpj { get; private set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public Address Address { get; private set; } = Address.Default();


        public static Company Create(string corporateName, string fantasyName, string description, string cnpj, string email, string phone, Address address)
        {
            var id = Guid.NewGuid();
            return new Company(id, corporateName, fantasyName, description, cnpj, email, phone, address);
        }
    }
}
