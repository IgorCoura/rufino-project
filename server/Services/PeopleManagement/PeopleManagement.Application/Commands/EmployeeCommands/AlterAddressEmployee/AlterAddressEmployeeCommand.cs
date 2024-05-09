namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee
{
    public record AlterAddressEmployeeCommand(Guid EmployeeId, Guid CompanyId, string ZipCode, string Street, string Number, 
        string Complement, string Neighborhood, string City, string State, string Country) : IRequest<AlterAddressEmployeeResponse>
    {
    }
}
