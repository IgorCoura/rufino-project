namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee
{
    public record AlterRoleEmployeeCommand(Guid Id, Guid RoleId) : IRequest<AlterRoleEmployeeResponse>;
}
