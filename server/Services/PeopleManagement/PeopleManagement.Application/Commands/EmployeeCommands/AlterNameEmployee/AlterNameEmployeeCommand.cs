using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee
{
    public record AlterNameEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Name) : IRequest<AlterNameEmployeeResponse>;
    public record AlterNameEmployeeModel(Guid EmployeeId, string Name)
    {
        public AlterNameEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, Name);
    }
}
