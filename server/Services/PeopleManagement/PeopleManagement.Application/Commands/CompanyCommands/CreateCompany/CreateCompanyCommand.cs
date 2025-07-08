using MediatR;

namespace PeopleManagement.Application.Commands.CompanyCommands.CreateCompany
{
    public record CreateCompanyCommand : IRequest<BaseDTO>
    {
        public CreateCompanyCommand(string? corporateName, string? fantasyName, string? cnpj, string? email, string? phone, string? zipCode, string? street, string? number, string? complement, string? neighborhood, string? city, string? state, string? country)
        {
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

        public string? CorporateName { get; }
        public string? FantasyName { get; }
        public string? Cnpj { get; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ZipCode { get; }
        public string? Street { get; }
        public string? Number { get; }
        public string? Complement { get; }
        public string? Neighborhood { get; }
        public string? City { get; }
        public string? State { get; }
        public string? Country { get; }

        public Company ToCompany(Guid id)
        {
            return Company.Create(
            id,
            this.CorporateName!,
            this.FantasyName!,
            this.Cnpj!,
            Contact.Create(
            this.Email!,
                this.Phone!),
            Address.Create(
                this.ZipCode!,
                this.Street!,
                this.Number!,
                this.Complement!,
                this.Neighborhood!,
                this.City!,
                this.State!,
                this.Country!)
            );
        }

    }
}
