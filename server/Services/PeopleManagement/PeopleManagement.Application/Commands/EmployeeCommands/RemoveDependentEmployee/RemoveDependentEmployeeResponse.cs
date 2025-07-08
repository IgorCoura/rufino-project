using PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.RemoveDependentEmployee
{
    public record RemoveDependentEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator RemoveDependentEmployeeResponse(Guid id) => new(id);
    }
}
