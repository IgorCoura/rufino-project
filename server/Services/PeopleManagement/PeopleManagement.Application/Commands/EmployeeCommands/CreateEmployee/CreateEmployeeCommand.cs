using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public record CreateEmployeeCommand(
        Guid CompanyId,
        string Name
        ) : IRequest<CreateEmployeeCommandResponse>;

}
