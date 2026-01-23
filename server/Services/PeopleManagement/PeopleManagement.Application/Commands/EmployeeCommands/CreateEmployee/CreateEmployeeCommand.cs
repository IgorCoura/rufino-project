using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public record CreateEmployeeCommand(
        Guid CompanyId,
        string Name,
        Guid RoleId,
        Guid WorkPlaceId
        ) : IRequest<CreateEmployeeResponse>
    {
        public Employee ToEmployee(Guid id) => Employee.Create(id, CompanyId, Name, RoleId, WorkPlaceId);
    }

    public record CreateEmployeeModel(string Name, Guid RoleId, Guid WorkPlaceId) 
    {
        public CreateEmployeeCommand ToCommand(Guid company) => new(company, Name, RoleId, WorkPlaceId);
    }

}
