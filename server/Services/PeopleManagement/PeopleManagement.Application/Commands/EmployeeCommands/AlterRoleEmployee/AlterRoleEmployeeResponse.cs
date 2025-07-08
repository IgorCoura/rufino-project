namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee
{
    public record AlterRoleEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterRoleEmployeeResponse(Guid input) =>
            new(input);
    }
}
