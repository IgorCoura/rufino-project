namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee
{
    public record AlterRoleEmployeeCommand(Guid EmployeeId, Guid CompanyId, Guid RoleId) : IRequest<AlterRoleEmployeeResponse>;
}
