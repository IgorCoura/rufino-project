using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee
{
    public record AlterNameEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Name) : IRequest<AlterNameEmployeeResponse>;
}
