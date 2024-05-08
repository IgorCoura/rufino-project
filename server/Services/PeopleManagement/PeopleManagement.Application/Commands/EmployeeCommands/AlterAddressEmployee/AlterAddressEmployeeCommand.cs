namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee
{
    public record AlterAddressEmployeeCommand(Guid Id, string ZipCode, string Street, string Number, 
        string Complement, string Neighborhood, string City, string State, string Country) : IRequest<AlterAddressEmployeeResponse>
    {
    }
}
