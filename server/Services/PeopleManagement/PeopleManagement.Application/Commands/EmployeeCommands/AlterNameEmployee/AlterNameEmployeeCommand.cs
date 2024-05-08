using PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterNameEmployee
{
    public record AlterNameEmployeeCommand(Guid Id, string Name) : IRequest<AlterNameEmployeeResponse>;
}
