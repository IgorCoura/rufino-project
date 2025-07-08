
namespace PeopleManagement.Application.Commands.CompanyCommands.EditCompany
{
    public record EditCompanyCommand : IRequest<EditCompanyResponse>
    {
        public EditCompanyCommand(
            Guid id, 
            string corporateName, 
            string fantasyName, 
            string cnpj, 
            string email, 
            string phone, 
            string zipCode, 
            string street, 
            string number, 
            string complement, 
            string neighborhood, 
            string city, 
            string state, 
            string country)
        {
            Id = id;
            CorporateName = corporateName;
            FantasyName = fantasyName;
            Cnpj = cnpj;
            Email = email;
            Phone = phone;
            ZipCode = zipCode;
            Street = street;
            Number = number;
            Complement = complement;
            Neighborhood = neighborhood;
            City = city;
            State = state;
            Country = country;
        }

        public Guid Id { get; }
        public string CorporateName { get; }
        public string FantasyName { get; }
        public string Cnpj { get; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ZipCode { get; }
        public string Street { get; }
        public string Number { get; }
        public string Complement { get; }
        public string Neighborhood { get; }
        public string City { get; }
        public string State { get; }
        public string Country { get; }
    }
}
