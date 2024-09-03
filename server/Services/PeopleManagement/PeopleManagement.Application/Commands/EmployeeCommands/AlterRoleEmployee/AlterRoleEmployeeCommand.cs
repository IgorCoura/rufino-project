namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee
{
    public record AlterRoleEmployeeCommand(Guid EmployeeId, Guid CompanyId, Guid RoleId) : IRequest<AlterRoleEmployeeResponse>;
    public record AlterRoleEmployeeModel(Guid EmployeeId, Guid RoleId)
    {
        public AlterRoleEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, RoleId);
    }
}
