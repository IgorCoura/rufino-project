namespace PeopleManagement.Application.Commands.WorkplaceCommands.CreateWorkplace
{
    public record CreateWorkplaceCommand(Guid CompanyId, string Name, CreateWorkplaceAddressModel Address): IRequest<CreateWorkplaceResponse>
    {
    }

    public record CreateWorkplaceModel(string Name, CreateWorkplaceAddressModel Address)
    {
        public CreateWorkplaceCommand ToCommand(Guid companyId) => new(companyId, Name, Address);
    }

    public record CreateWorkplaceAddressModel(
        string ZipCode,
        string Street,
        string Number,
        string Complement,
        string Neighborhood,
        string City,
        string State,
        string Country)
    {
    }
}
