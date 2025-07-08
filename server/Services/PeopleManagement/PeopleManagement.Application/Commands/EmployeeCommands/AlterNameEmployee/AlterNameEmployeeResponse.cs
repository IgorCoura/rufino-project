namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee
{
    public record AlterNameEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterNameEmployeeResponse(Guid id) => new(id);
    }
}
