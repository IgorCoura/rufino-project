using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.RemoveDependentEmployee
{
    public record RemoveDependentEmployeeCommand(Guid EmployeeId, Guid CompanyId, string NameDepedent) : IRequest<RemoveDependentEmployeeResponse>
    {
    }
    public record RemoveDependentEmployeeModel(Guid EmployeeId, string NameDepedent)
    {
        public RemoveDependentEmployeeCommand ToCommand(Guid companyId) => new(this.EmployeeId, companyId, NameDepedent);
    }
}
