namespace PeopleManagement.Application.Commands.CreateCompany
{
    public record CreateCompanyCommand : IRequest<BaseDTO>
    {        
        public string CorporateNmae { get; } = string.Empty;
        public string FantasyName { get; } = string.Empty;
        public string Cnpj { get; } = string.Empty;
        public string ZipCode { get; } = string.Empty;
        public string Street { get; } = string.Empty;
        public string Number { get; } = string.Empty;
        public string Complement { get; } = string.Empty;
        public string Neighborhood { get; } = string.Empty;
        public string City { get; } = string.Empty;
        public string State { get; } = string.Empty;
        public string Country { get; } = string.Empty;

        public CreateCompanyCommand(string corporateNmae, string fantasyName, string cnpj, 
            string zipCode, string street, string number, string complement, string neighborhood, 
            string city, string state, string country)
        {
            CorporateNmae = corporateNmae;
            FantasyName = fantasyName;
            Cnpj = cnpj;
            ZipCode = zipCode;
            Street = street;
            Number = number;
            Complement = complement;
            Neighborhood = neighborhood;
            City = city;
            State = state;
            Country = country;
        }

    }
}
