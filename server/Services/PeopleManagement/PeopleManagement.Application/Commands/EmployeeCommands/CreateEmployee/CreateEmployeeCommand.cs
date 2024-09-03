using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public record CreateEmployeeCommand(
        Guid CompanyId,
        string Name
        ) : IRequest<CreateEmployeeResponse>
    {
        public Employee ToEmployee(Guid id) => Employee.Create(id, CompanyId, Name);
    }

    public record CreateEmployeeModel(string Name) 
    {
        public CreateEmployeeCommand ToCommand(Guid company) => new(company, Name);
    }

}
