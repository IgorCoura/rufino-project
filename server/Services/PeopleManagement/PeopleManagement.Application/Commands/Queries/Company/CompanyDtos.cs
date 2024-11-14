namespace PeopleManagement.Application.Commands.Queries.Company
{
    public record CompanySimplefiedDTO
    {
        public Guid Id { get; init; }
        public string CorporateName { get; init; } = null!;
        public string FantasyName { get; init; } = null!;
        public string Cnpj { get; init; }


    }
}
