namespace PeopleManagement.Application.Queries.Company
{
    public record CompanySimplefiedDTO
    {
        public Guid Id { get; init; }
        public string CorporateName { get; init; } = null!;
        public string FantasyName { get; init; } = null!;
        public string Cnpj { get; init; } = null!;
    }

    public record class CompanyDto
    {
        public Guid Id { get; init; }
        public string CorporateName { get; init; } = null!;
        public string FantasyName { get; init; } = null!;
        public string Cnpj { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Phone { get; init; } = null!;
        public string ZipCode { get; init; } = null!;
        public string Street { get; init; } = null!;
        public string Number { get; init; } = null!;
        public string Complement { get; init; } = null!;
        public string Neighborhood { get; init; } = null!;
        public string City { get; init; } = null!;
        public string State { get; init; } = null!;
        public string Country { get; init; } = null!;
    }

}   
