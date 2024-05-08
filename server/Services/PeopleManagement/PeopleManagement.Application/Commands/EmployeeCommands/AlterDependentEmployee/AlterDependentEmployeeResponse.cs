namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public record AlterDependentEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator AlterDependentEmployeeResponse(Guid id) => new(id);
    }
}
