using PeopleManagement.Application.Commands.WorkplaceCommands.CreateWorkplace;

namespace PeopleManagement.Application.Commands.WorkplaceCommands.EditWorkplace
{
    public record EditWorkplaceCommand(Guid Id, Guid CompanyId, string Name, EditWorkplaceAddressModel Address) : IRequest<EditWorkplaceResponse>
    {
    }
    public record EditWorkplaceModel(Guid Id, string Name,EditWorkplaceAddressModel Address)
    {
        public EditWorkplaceCommand ToCommand(Guid companyId) => new(Id, companyId, Name, Address);
    }

    public record EditWorkplaceAddressModel(
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
