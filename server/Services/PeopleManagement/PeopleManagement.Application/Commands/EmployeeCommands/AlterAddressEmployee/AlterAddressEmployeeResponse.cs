namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee
{
    public record AlterAddressEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterAddressEmployeeResponse(Guid id) => new(id);
    }
}
